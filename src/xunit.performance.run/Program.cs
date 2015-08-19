using Microsoft.Diagnostics.Tracing;
using Microsoft.Xunit.Performance.Sdk;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance
{
    class Program
    {
        class ConsoleReporter : IMessageSink
        {
            public bool OnMessage(IMessageSinkMessage message)
            {
                Console.WriteLine(message.ToString());
                return true;
            }
        }

        static int Main(string[] args)
        {
            var project = ParseCommandLine(args);
            var tests = DiscoverTests(project.Assemblies, project.Filters, new ConsoleReporter());

            if (!Directory.Exists(project.OutputDir))
                Directory.CreateDirectory(project.OutputDir);

            RunTests(tests, project.RunnerCommand, project.RunName, project.OutputDir);

            return 0;
        }

        const string RunnerOptions = "-nologo -parallel none -noshadow -noappdomain -verbose";

        private static void RunTests(IEnumerable<PerformanceTestInfo> tests, string runnerCommand, string runId, string outDir)
        {
            string etlPath = Path.Combine(outDir, runId + ".etl");
            string xmlPath = Path.Combine(outDir, runId + ".xml");

            var providers =
                from test in tests
                from metric in test.Metrics
                from provider in metric.ProviderInfo
                select provider;

            using (ETWLogging.StartAsync(etlPath, providers).Result)
            {
                const int maxCommandLineLength = 32767;

                var outputOption = "-xml " + xmlPath;

                var allMethods = new HashSet<string>();

                var assemblyFileBatch = new HashSet<string>();
                var methodBatch = new HashSet<string>();
                var commandLineLength = runnerCommand.Length + " ".Length + RunnerOptions.Length + " ".Length + outputOption.Length;

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
                            RunTestBatch(methodBatch, assemblyFileBatch, runnerCommand, runId, outputOption);
                            methodBatch.Clear();
                            assemblyFileBatch.Clear();
                        }

                        methodBatch.Add(methodName);
                        assemblyFileBatch.Add(currentTestInfo.Assembly.AssemblyFilename);
                    }
                }

                if (methodBatch.Count > 0)
                    RunTestBatch(methodBatch, assemblyFileBatch, runnerCommand, runId, outputOption);
            }


            using (var source = new ETWTraceEventSource(etlPath))
            {
                if (source.EventsLost > 0)
                    throw new Exception($"Events were lost in trace '{etlPath}'");

                using (var evaluationContext = new PerformanceMetricEvaluationContextImpl(source, tests))
                {
                    source.Process();


                    var xmlDoc = XDocument.Load(xmlPath);
                    foreach (var testElem in xmlDoc.Descendants("test"))
                    {
                        var iterations = evaluationContext.GetValues(testElem.Attribute("name").Value);
                        if (iterations != null)
                        {
                            var iterationsElem = new XElement("iterations");
                            testElem.Add(iterationsElem);

                            for (int i = 0; i < iterations.Count; i++)
                            {
                                var iteration = iterations[i];
                                if (iteration != null)
                                {
                                    var iterationElem = new XElement("iteration", new XAttribute("index", i));
                                    iterationsElem.Add(iterationElem);

                                    foreach (var value in iteration)
                                    {
                                        iterationElem.Add(
                                            new XElement("value", 
                                                new XAttribute("name", value.Name), 
                                                new XAttribute("unit", value.Unit), 
                                                value.Value));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void RunTestBatch(IEnumerable<string> methods, IEnumerable<string> assemblyFiles, string runnerCommand, string runId, string outputOption)
        {
            var commandLineArgs = new StringBuilder();
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
            commandLineArgs.Append(RunnerOptions);
            commandLineArgs.Append(" ");
            commandLineArgs.Append(outputOption);

            Environment.SetEnvironmentVariable("XUNIT_PERFORMANCE_RUN_ID", runId);
            Environment.SetEnvironmentVariable("XUNIT_PERFORMANCE_MAX_ITERATION", 1000.ToString());
            Environment.SetEnvironmentVariable("XUNIT_PERFORMANCE_MAX_TOTAL_MILLISECONDS", 1000.ToString());

            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = runnerCommand,
                Arguments = commandLineArgs.ToString(),
                UseShellExecute = false,
            };

            using (var proc = Process.Start(startInfo))
            {
                proc.EnableRaisingEvents = true;
                proc.WaitForExit();
            }
        }

        private static IEnumerable<PerformanceTestInfo> DiscoverTests(IEnumerable<XunitProjectAssembly> assemblies, XunitFilters filters, IMessageSink diagnosticMessageSink)
        {
            var tests = new List<PerformanceTestInfo>();

            foreach (var assembly in assemblies)
            {
                // Note: We do not use shadowCopy because that creates a new AppDomain which can cause
                // assembly load failures with delay-signed or "fake signed" assemblies.
                using (var controller = new XunitFrontController(
                    assemblyFileName: assembly.AssemblyFilename,
                    shadowCopy: false,
                    useAppDomain: false,
                    diagnosticMessageSink: new ConsoleDiagnosticsMessageVisitor())
                    )
                using (var discoveryVisitor = new PerformanceTestDiscoveryVisitor(assembly, filters, diagnosticMessageSink))
                {
                    controller.Find(includeSourceInformation: false, messageSink: discoveryVisitor, discoveryOptions: TestFrameworkOptions.ForDiscovery());
                    discoveryVisitor.Finished.WaitOne();
                    tests.AddRange(discoveryVisitor.Tests);
                }
            }

            return tests;
        }

        private static XunitPerformanceProject ParseCommandLine(string[] args)
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

                if (optionName == "trait")
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
                else if (optionName == "notrait")
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
                else if (optionName == "class")
                {
                    if (option.Value == null)
                        throw new ArgumentException("missing argument for -class");

                    project.Filters.IncludedClasses.Add(option.Value);
                }
                else if (optionName == "method")
                {
                    if (option.Value == null)
                        throw new ArgumentException("missing argument for -method");

                    project.Filters.IncludedMethods.Add(option.Value);
                }
                else if (optionName == "runner")
                {
                    if (option.Value == null)
                        throw new ArgumentException("missing argument for -runner");

                    project.RunnerCommand = option.Value;
                }
                else if (optionName == "baselinerunner")
                {
                    if (option.Value == null)
                        throw new ArgumentException("missing argument for -baselineRunner");

                    project.BaselineRunnerCommand = option.Value;
                }
                else if (optionName == "baseline")
                {
                    if (option.Value == null)
                        throw new ArgumentException("missing argument for -baseline");

                    AddBaseline(project, option.Value);
                }
                else if (optionName == "runname")
                {
                    if (option.Value == null)
                        throw new ArgumentException("missing argument for -runName");

                    project.RunName = option.Value;
                }
                else if (optionName == "outdir")
                {
                    if (option.Value == null)
                        throw new ArgumentException("missing argument for -outDir");

                    project.OutputDir = option.Value;
                }
                else
                {
                    if (option.Value == null)
                        throw new ArgumentException($"missing filename for {option.Key}");

                    project.Output.Add(optionName, option.Value);
                }
            }

            return project;
        }

        static bool IsConfigFile(string fileName)
        {
            return fileName.EndsWith(".config", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
        }

        static XunitPerformanceProject GetProjectFile(List<Tuple<string, string>> assemblies)
        {
            var result = new XunitPerformanceProject();

            foreach (var assembly in assemblies)
                result.Add(new XunitProjectAssembly
                {
                    AssemblyFilename = Path.GetFullPath(assembly.Item1),
                    ConfigFilename = assembly.Item2 != null ? Path.GetFullPath(assembly.Item2) : null,
                    ShadowCopy = true
                });

            return result;
        }

        static void AddBaseline(XunitPerformanceProject project, string assembly)
        {
            project.AddBaseline(new XunitProjectAssembly
            {
                AssemblyFilename = Path.GetFullPath(assembly),
                ShadowCopy = true,
            });
        }

        private static KeyValuePair<string, string> PopOption(Stack<string> arguments)
        {
            var option = arguments.Pop();
            string value = null;

            if (arguments.Count > 0 && !arguments.Peek().StartsWith("-", StringComparison.Ordinal))
                value = arguments.Pop();

            return new KeyValuePair<string, string>(option, value);
        }

        static void GuardNoOptionValue(KeyValuePair<string, string> option)
        {
            if (option.Value != null)
                throw new ArgumentException($"error: unknown command line option: {option.Value}");
        }
    }
}
