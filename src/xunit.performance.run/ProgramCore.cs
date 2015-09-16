﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance.Sdk;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance
{
    internal abstract class ProgramCore
    {
        private class ConsoleReporter : IMessageSink
        {
            public bool OnMessage(IMessageSinkMessage message)
            {
                Console.WriteLine(message.ToString());
                return true;
            }
        }

        private bool _nologo = false;
        private bool _verbose = false;

        internal abstract IPerformanceMetricReader GetPerformanceMetricReader(IEnumerable<PerformanceTestInfo> tests, string pathBase, string runId);

        internal abstract IDisposable StartTracing(IEnumerable<PerformanceTestInfo> tests, string pathBase);

        internal abstract string GetRuntimeVersion();

        internal int Run(string[] args)
        {
            if (args.Length == 0 || args[0] == "-?")
            {
                PrintHeader();
                PrintUsage();
                return 1;
            }

            try
            {
                var project = ParseCommandLine(args);
                if (!_nologo)
                {
                    PrintHeader();
                }

                var tests = DiscoverTests(project.Assemblies, project.Filters, new ConsoleReporter());
                PrintIfVerbose($"Creating output directory: {project.OutputDir}");
                if (!Directory.Exists(project.OutputDir))
                    Directory.CreateDirectory(project.OutputDir);

                if (!tests.Any())
                {
                    Console.WriteLine("Could not find any suitable tests.");
                }
                else
                {
                    RunTests(tests, project.RunnerHost, project.RunnerCommand, project.RunnerArgs, project.RunId, project.OutputDir);
                }
            }
            catch (Exception ex)
            {
                Console.Error.Write("Error: ");
                ReportExceptionToStderr(ex);
            }

            return 0;
        }

        private void RunTests(IEnumerable<PerformanceTestInfo> tests, string runnerHost, string runnerCommand, string runnerArgs, string runId, string outDir)
        {
            string pathBase = Path.Combine(outDir, runId);
            string xmlPath = pathBase + ".xml";

            using (StartTracing(tests, pathBase))
            {
                const int maxCommandLineLength = 32767;

                var outputOption = "-xml " + xmlPath;

                var allMethods = new HashSet<string>();

                var assemblyFileBatch = new HashSet<string>();
                var methodBatch = new HashSet<string>();
                var commandLineLength = (runnerHost?.Length ?? 0) + runnerCommand.Length + " ".Length + (runnerArgs?.Length ?? 0) + " ".Length + outputOption.Length;

                foreach (var currentTestInfo in tests)
                {
                    var methodName = currentTestInfo.TestCase.TestMethod.TestClass.Class.Name + "." + currentTestInfo.TestCase.TestMethod.Method.Name;
                    if (allMethods.Add(methodName))
                    {
                        var currentTestInfoCommandLineLength = "-method ".Length + methodName.Length + " ".Length;
                        if (!assemblyFileBatch.Contains(currentTestInfo.Assembly.AssemblyFilename))
                            currentTestInfoCommandLineLength += "\"".Length + currentTestInfo.Assembly.AssemblyFilename.Length + "\" ".Length;

                        if (commandLineLength + currentTestInfoCommandLineLength > maxCommandLineLength)
                        {
                            RunTestBatch(methodBatch, assemblyFileBatch, runnerHost, runnerCommand, runnerArgs, runId, outputOption);
                            methodBatch.Clear();
                            assemblyFileBatch.Clear();
                        }

                        methodBatch.Add(methodName);
                        assemblyFileBatch.Add(currentTestInfo.Assembly.AssemblyFilename);
                    }
                }

                if (methodBatch.Count > 0)
                    RunTestBatch(methodBatch, assemblyFileBatch, runnerHost, runnerCommand, runnerArgs, runId, outputOption);
            }

            using (var evaluationContext = GetPerformanceMetricReader(tests, pathBase, runId))
            {
                var xmlDoc = XDocument.Load(xmlPath);
                foreach (var testElem in xmlDoc.Descendants("test"))
                {
                    var testName = testElem.Attribute("name").Value;

                    var perfElem = new XElement("performance", new XAttribute("runid", runId), new XAttribute("etl", Path.GetFullPath(evaluationContext.LogPath)));
                    testElem.Add(perfElem);

                    var metrics = evaluationContext.GetMetrics(testName);
                    if (metrics != null)
                    {
                        var metricsElem = new XElement("metrics");
                        perfElem.Add(metricsElem);

                        foreach (var metric in metrics)
                            metricsElem.Add(new XElement(metric.Id, new XAttribute("displayName", metric.DisplayName), new XAttribute("unit", metric.Unit)));
                    }

                    var iterations = evaluationContext.GetValues(testName);
                    if (iterations != null)
                    {
                        var iterationsElem = new XElement("iterations");
                        perfElem.Add(iterationsElem);

                        for (int i = 0; i < iterations.Count; i++)
                        {
                            var iteration = iterations[i];
                            if (iteration != null)
                            {
                                var iterationElem = new XElement("iteration", new XAttribute("index", i));
                                iterationsElem.Add(iterationElem);

                                foreach (var value in iteration)
                                    iterationElem.Add(new XAttribute(value.Key, value.Value.ToString("R")));
                            }
                        }
                    }
                }

                using (var xmlFile = File.Create(xmlPath))
                    xmlDoc.Save(xmlFile);
            }
        }

        private void RunTestBatch(IEnumerable<string> methods, IEnumerable<string> assemblyFiles, string runnerHost, string runnerCommand, string runnerArgs, string runId, string outputOption)
        {
            var commandLineArgs = new StringBuilder();

            if (!string.IsNullOrEmpty(runnerHost))
            {
                commandLineArgs.Append(runnerCommand);
                commandLineArgs.Append(" ");
            }

            foreach (var assemblyFile in assemblyFiles)
            {
                commandLineArgs.Append(assemblyFile);
                commandLineArgs.Append(" ");
            }
            foreach (var method in methods)
            {
                commandLineArgs.Append("-method ");
                commandLineArgs.Append(method);
                commandLineArgs.Append(" ");
            }

            if (!string.IsNullOrEmpty(runnerArgs))
            {
                commandLineArgs.Append(runnerArgs);
                commandLineArgs.Append(" ");
            }

            commandLineArgs.Append(outputOption);

            Environment.SetEnvironmentVariable("XUNIT_PERFORMANCE_RUN_ID", runId);
            Environment.SetEnvironmentVariable("XUNIT_PERFORMANCE_MAX_ITERATION", 1000.ToString());
            Environment.SetEnvironmentVariable("XUNIT_PERFORMANCE_MAX_TOTAL_MILLISECONDS", 1000.ToString());

            Environment.SetEnvironmentVariable("COMPLUS_gcConcurrent", "0");
            Environment.SetEnvironmentVariable("COMPLUS_gcServer", "0");

            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = runnerHost ?? runnerCommand,
                Arguments = commandLineArgs.ToString(),
                UseShellExecute = false,
            };

            PrintIfVerbose($@"Launching runner:
Runner:    {startInfo.FileName}
Arguments: {startInfo.Arguments}");

            try
            {
                using (var proc = Process.Start(startInfo))
                {
                    proc.EnableRaisingEvents = true;
                    proc.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Could not launch the test runner, {startInfo.FileName}", innerException: ex);
            }
        }

        private IEnumerable<PerformanceTestInfo> DiscoverTests(IEnumerable<XunitProjectAssembly> assemblies, XunitFilters filters, IMessageSink diagnosticMessageSink)
        {
            var tests = new List<PerformanceTestInfo>();

            foreach (var assembly in assemblies)
            {
                PrintIfVerbose($"Discovering tests for {assembly.AssemblyFilename}.");

                // Note: We do not use shadowCopy because that creates a new AppDomain which can cause
                // assembly load failures with delay-signed or "fake signed" assemblies.
                using (var controller = new XunitFrontController(
                    assemblyFileName: assembly.AssemblyFilename,
                    shadowCopy: false,
                    appDomainSupport: AppDomainSupport.Denied,
                    diagnosticMessageSink: new ConsoleDiagnosticsMessageVisitor())
                    )
                using (var discoveryVisitor = new PerformanceTestDiscoveryVisitor(assembly, filters, diagnosticMessageSink))
                {
                    controller.Find(includeSourceInformation: false, messageSink: discoveryVisitor, discoveryOptions: TestFrameworkOptions.ForDiscovery());
                    discoveryVisitor.Finished.WaitOne();
                    tests.AddRange(discoveryVisitor.Tests);
                }
            }

            PrintIfVerbose($"Discovered a total of {tests.Count} tests.");
            return tests;
        }

        private XunitPerformanceProject ParseCommandLine(string[] args)
        {
            var arguments = new Stack<string>();
            for (var i = args.Length - 1; i >= 0; i--)
                arguments.Push(args[i]);

            var assemblies = new List<Tuple<string, string>>();

            while (arguments.Count > 0)
            {
                if (arguments.Peek().StartsWith("-", StringComparison.Ordinal))
                    break;

                var assemblyFile = arguments.Pop();

                if (IsConfigFile(assemblyFile))
                    throw new ArgumentException($"expecting assembly, got config file: {assemblyFile}");
                if (!File.Exists(assemblyFile))
                    throw new ArgumentException($"file not found: {assemblyFile}");

                string configFile = null;
                if (arguments.Count > 0)
                {
                    var value = arguments.Peek();
                    if (!value.StartsWith("-", StringComparison.Ordinal) && IsConfigFile(value))
                    {
                        configFile = arguments.Pop();
                        if (!File.Exists(configFile))
                            throw new ArgumentException($"config file not found: {configFile}");
                    }
                }

                assemblies.Add(Tuple.Create(assemblyFile, configFile));
            }

            if (assemblies.Count == 0)
                throw new ArgumentException("must specify at least one assembly");

            var project = GetProjectFile(assemblies);

            while (arguments.Count > 0)
            {
                var option = PopOption(arguments);
                var optionName = option.Key.ToLowerInvariant();

                if (!optionName.StartsWith("-", StringComparison.Ordinal))
                    throw new ArgumentException($"unknown command line option: {option.Key}");

                optionName = optionName.Substring(1);

                switch (optionName)
                {
                    case "nologo":
                        _nologo = true;
                        break;

                    case "verbose":
                        _verbose = true;
                        break;

                    case "trait":
                        {
                            if (option.Value == null)
                                throw new ArgumentException("missing argument for -trait");

                            var pieces = option.Value.Split('=');
                            if (pieces.Length != 2 || string.IsNullOrEmpty(pieces[0]) || string.IsNullOrEmpty(pieces[1]))
                                throw new ArgumentException("incorrect argument format for -trait (should be \"name=value\")");

                            var name = pieces[0];
                            var value = pieces[1];
                            project.Filters.IncludedTraits.Add(name, value);
                        }
                        break;

                    case "notrait":
                        {
                            if (option.Value == null)
                                throw new ArgumentException("missing argument for -notrait");

                            var pieces = option.Value.Split('=');
                            if (pieces.Length != 2 || string.IsNullOrEmpty(pieces[0]) || string.IsNullOrEmpty(pieces[1]))
                                throw new ArgumentException("incorrect argument format for -notrait (should be \"name=value\")");

                            var name = pieces[0];
                            var value = pieces[1];
                            project.Filters.ExcludedTraits.Add(name, value);
                        }
                        break;

                    case "class":
                        if (option.Value == null)
                            throw new ArgumentException("missing argument for -class");

                        project.Filters.IncludedClasses.Add(option.Value);
                        break;

                    case "method":
                        if (option.Value == null)
                            throw new ArgumentException("missing argument for -method");

                        project.Filters.IncludedMethods.Add(option.Value);
                        break;

                    case "runnerhost":
                        if (option.Value == null)
                            throw new ArgumentException("missing argument for -runnerhost");

                        project.RunnerHost = option.Value;
                        break;

                    case "runner":
                        if (option.Value == null)
                            throw new ArgumentException("missing argument for -runner");

                        project.RunnerCommand = option.Value;
                        break;

                    case "runnerargs":
                        if (option.Value == null)
                            throw new ArgumentException("missing argument for -runnerargs");

                        project.RunnerArgs = option.Value;
                        break;

                    case "baselinerunner":
                        if (option.Value == null)
                            throw new ArgumentException("missing argument for -baselineRunner");

                        project.BaselineRunnerCommand = option.Value;
                        break;

                    case "baseline":
                        if (option.Value == null)
                            throw new ArgumentException("missing argument for -baseline");

                        AddBaseline(project, option.Value);
                        break;

                    case "runid":
                        if (option.Value == null)
                            throw new ArgumentException("missing argument for -runid");

                        if (option.Value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                            throw new ArgumentException($"runname contains invalid characters.", optionName);

                        project.RunId = option.Value;
                        break;

                    case "outdir":
                        if (option.Value == null)
                            throw new ArgumentException("missing argument for -outdir");

                        if (option.Value.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                            throw new ArgumentException($"outdir contains invalid characters.", optionName);

                        project.OutputDir = option.Value;
                        break;

                    default:
                        if (option.Value == null)
                            throw new ArgumentException($"missing filename for {option.Key}");

                        project.Output.Add(optionName, option.Value);
                        break;
                }
            }

            return project;
        }

        private static bool IsConfigFile(string fileName)
        {
            return fileName.EndsWith(".config", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
        }

        private static XunitPerformanceProject GetProjectFile(List<Tuple<string, string>> assemblies)
        {
            var result = new XunitPerformanceProject();

            foreach (var assembly in assemblies)
                result.Add(new XunitProjectAssembly
                {
                    AssemblyFilename = Path.GetFullPath(assembly.Item1),
                    ConfigFilename = assembly.Item2 != null ? Path.GetFullPath(assembly.Item2) : null,
                });

            return result;
        }

        private static void AddBaseline(XunitPerformanceProject project, string assembly)
        {
            project.AddBaseline(new XunitProjectAssembly
            {
                AssemblyFilename = Path.GetFullPath(assembly),
            });
        }

        private static KeyValuePair<string, string> PopOption(Stack<string> arguments)
        {
            var option = arguments.Pop();
            string value = null;

            if (option.Equals("-runnerargs", StringComparison.OrdinalIgnoreCase))
            {
                // Special case, just grab all of the args to pass along.
                if (arguments.Count > 0)
                {
                    value = arguments.Pop();
                }
            }

            if (arguments.Count > 0 && !arguments.Peek().StartsWith("-", StringComparison.Ordinal))
                value = arguments.Pop();

            return new KeyValuePair<string, string>(option, value);
        }

        private static void GuardNoOptionValue(KeyValuePair<string, string> option)
        {
            if (option.Value != null)
                throw new ArgumentException($"error: unknown command line option: {option.Value}");
        }

        private static void ReportException(Exception ex, TextWriter writer)
        {
            for (; ex != null; ex = ex.InnerException)
            {
                writer.WriteLine(ex.Message);
                writer.WriteLine(ex.StackTrace);
            }
        }

        private static void ReportExceptionToStderr(Exception ex)
        {
            ReportException(ex, Console.Error);
        }

        private void PrintHeader()
        {
            Console.WriteLine($"xunit.performance Console Runner ({IntPtr.Size * 8}-bit .NET {GetRuntimeVersion()})");
            Console.WriteLine("Copyright (C) 2015 Microsoft Corporation.");
            Console.WriteLine();
        }

        protected void PrintIfVerbose(string message)
        {
            if (_verbose)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine(@"usage: xunit.performance.run <assemblyFile> [options]

Valid options:
  -nologo                : do not show the copyright message
  -trait ""name = value""  : only run tests with matching name/value traits
                         : if specified more than once, acts as an OR operation
  -notrait ""name=value""  : do not run tests with matching name/value traits
                         : if specified more than once, acts as an AND operation
  -class ""name""          : run all methods in a given test class (should be fully
                         : specified; i.e., 'MyNamespace.MyClass')
                         : if specified more than once, acts as an OR operation
  -method ""name""         : run a given test method (should be fully specified;
                         : i.e., 'MyNamespace.MyClass.MyTestMethod')
  -runnerhost ""name""   : use the given CLR host to launch the runner program.
  -runner ""name""         : use the specified runner to excecute tests. Defaults
                         : to xunit.console.exe
  -runnerargs ""args""   : append the given args to the end of the xunit runner's command-line
                           : a quoted group of arguments, 
                           : e.g. -runnerargs ""-verbose -nologo -parallel none""
  -runid ""name""        : a run identifier used to create unique output filenames.
  -outdir  ""name""        : folder for output files.
  -verbose               : verbose logging
");
        }
    }
}