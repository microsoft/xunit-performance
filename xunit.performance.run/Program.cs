using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance
{
    class Program
    {
        static int Main(string[] args)
        {
            var project = ParseCommandLine(args);

            //foreach (var test in DiscoverTests(project.Assemblies, project.Filters))
            //{
            //    Console.WriteLine(test.DisplayName);
            //}

            var runnerArgs = new StringBuilder();
            foreach (var assembly in project.Assemblies)
            {
                runnerArgs.Append(assembly.AssemblyFilename);
                runnerArgs.Append(" ");
            }

            foreach (var trait in project.Filters.IncludedTraits)
                foreach (var traitVal in trait.Value)
                    runnerArgs.Append($"-trait \"{trait.Key}={traitVal}\" ");

            foreach (var trait in project.Filters.ExcludedTraits)
                foreach (var traitVal in trait.Value)
                    runnerArgs.Append($"-notrait \"{trait.Key}={traitVal}\" ");

            foreach (var method in project.Filters.IncludedMethods)
                runnerArgs.Append($"-method \"{method}\" ");

            foreach (var includeClass in project.Filters.IncludedClasses)
                runnerArgs.Append($"-class \"{includeClass}\" ");

            runnerArgs.Append("-nologo -verbose");

            using (ETWLogging.StartAsync(Path.Combine(project.EtlDirectory, project.RunName + ".etl")).Result)
            {
                Environment.SetEnvironmentVariable("XUNIT_PERFORMANCE_RUN_ID", project.RunName);

                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = project.RunnerCommand,
                    Arguments = runnerArgs.ToString(),
                    CreateNoWindow = false,
                    UseShellExecute = false,
                };

                using (var proc = Process.Start(startInfo))
                {
                    proc.EnableRaisingEvents = true;
                    proc.WaitForExit();
                    return proc.ExitCode;
                }
            }
        }


        private static IEnumerable<ITestCase> DiscoverTests(IEnumerable<XunitProjectAssembly> assemblies, XunitFilters filters)
        {
            List<ITestCase> tests = new List<ITestCase>();

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
                using (var discoveryVisitor = new TestCaseDisoveryVisitor())
                {
                    controller.Find(includeSourceInformation: false, messageSink: discoveryVisitor, discoveryOptions: TestFrameworkOptions.ForDiscovery());
                    discoveryVisitor.Finished.WaitOne();
                    tests.AddRange(discoveryVisitor.Tests.Where(testCase => string.IsNullOrEmpty(testCase.SkipReason) && filters.Filter(testCase)));
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
                }
                else if (optionName == "etldir")
                {
                    if (option.Value == null)
                        throw new ArgumentException("missing argument for -etlDir");
                    if (!Directory.Exists(option.Value))
                        throw new ArgumentException($"directory not found: {option.Value}");

                    project.EtlDirectory = option.Value;
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

    internal static class DictionaryExtensions
    {
        public static void Add<TKey, TValue>(this IDictionary<TKey, List<TValue>> dictionary, TKey key, TValue value)
        {
            dictionary.GetOrAdd(key).Add(value);
        }

        public static bool Contains<TKey, TValue>(this IDictionary<TKey, List<TValue>> dictionary, TKey key, TValue value, IEqualityComparer<TValue> valueComparer)
        {
            List<TValue> values;

            if (!dictionary.TryGetValue(key, out values))
                return false;

            return values.Contains(value, valueComparer);
        }

        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
            where TValue : new()
        {
            return dictionary.GetOrAdd<TKey, TValue>(key, () => new TValue());
        }

        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> newValue)
        {
            TValue result;

            if (!dictionary.TryGetValue(key, out result))
            {
                result = newValue();
                dictionary[key] = result;
            }

            return result;
        }
    }
}
