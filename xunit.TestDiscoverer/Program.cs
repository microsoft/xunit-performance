using System;
using System.Collections.Generic;
using System.IO;
using Xunit.Abstractions;

namespace Xunit.TestDiscoverer
{
    /// <summary>
    /// Finds (but does not run) all XUnit tests in the given assembly.
    /// </summary>
    internal class Program
    {
        private static readonly XunitFilters _filter = new XunitFilters();

        internal static void Main(string[] args)
        {
            IEnumerable<string> assemblyFileNames;

            try
            {
                assemblyFileNames = ParseCommandLine(args);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Command line error: {ex.Message}");
                PrintUsage();
                return;
            }

            foreach (var assemblyFileName in assemblyFileNames)
            {
                // Console.Error.WriteLine($"Processing {assemblyFileName}");
                DiscoverTests(assemblyFileName, TestCaseDiscovered);
            }
        }

        private static IEnumerable<string> ParseCommandLine(string[] args)
        {
            var assemblyFileNames = new List<string>();

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-") || args[i].StartsWith("/"))
                {
                    string switchName = args[i].Substring(1).ToLowerInvariant();
                    switch (switchName)
                    {
                        case "trait":
                        {
                            if (++i >= args.Length)
                                throw new ArgumentException("Missing argument for -trait");

                            var pieces = args[i].Split('=');
                            if (pieces.Length != 2 || string.IsNullOrEmpty(pieces[0]) || string.IsNullOrEmpty(pieces[1]))
                                throw new ArgumentException("Incorrect argument format for -trait (should be \"name=value\")");

                            _filter.IncludedTraits.Add(pieces[0], new List<string> { pieces[1] });
                            break;
                        }

                        case "notrait":
                        {
                            if (++i >= args.Length)
                                throw new ArgumentException("Missing argument for -notrait");

                            var pieces = args[i].Split('=');
                            if (pieces.Length != 2 || string.IsNullOrEmpty(pieces[0]) || string.IsNullOrEmpty(pieces[1]))
                                throw new ArgumentException("Incorrect argument format for -trait (should be \"name=value\")");

                            _filter.ExcludedTraits.Add(pieces[0], new List<string> { pieces[1] });
                            break;
                        }

                        default:
                            throw new ArgumentException($"Unrecognized switch: {args[i]}");
                    }
                }
                else
                {
                    assemblyFileNames.AddRange(ExpandFilePath(args[i]));
                }
            }

            if (assemblyFileNames.Count == 0)
            {
                throw new ArgumentException("Expected one or more assembly names on the command line.");
            }

            return assemblyFileNames;
        }

        private static bool DiscoverTests(string assemblyFileName, Action<ITestCase> testCaseDiscoveredAction)
        {
            try
            {
                // Note: We do not use shadowCopy because that creates a new AppDomain which can cause
                // assembly load failures with delay-signed or "fake signed" assemblies.
                using (var controller = new XunitFrontController(
                    assemblyFileName: assemblyFileName,
                    shadowCopy: false,
                    diagnosticMessageSink: new ConsoleDiagnosticsMessageVisitor())
                    )
                using (var discoveryVisitor = new TestCaseDisoveryVisitor(testCaseDiscoveredAction))
                {
                    controller.Find(includeSourceInformation: false, messageSink: discoveryVisitor, discoveryOptions: TestFrameworkOptions.ForDiscovery());
                    discoveryVisitor.Finished.WaitOne();
                }
            }
            catch (Exception ex)
            {
                PrintException(ex);
                return false;
            }

            return true;
        }

        private static bool AcceptFilter(ITestCase testCase)
        {
            return string.IsNullOrEmpty(testCase.SkipReason) && _filter.Filter(testCase);
        }

        private static void TestCaseDiscovered(ITestCase testCase)
        {
            if (AcceptFilter(testCase))
            {
                // Note: Don't use DisplayName here because, while it defaults to the fully-qualified
                // name of the test method, it can be overridden.
                Console.WriteLine($"{testCase.TestMethod.TestClass.Class.Name}.{testCase.TestMethod.Method.Name}");
            }
        }

        private static void PrintException(Exception ex)
        {
            for (; ex != null; ex = ex.InnerException)
            {
                Console.Error.WriteLine($"{ex.GetType().FullName}: {ex.Message}");
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Discovers all the XUnit test cases in an assembly.");
            Console.WriteLine("Usage:");
            Console.WriteLine("xunit.TestDiscoverer <assemblyPath> [options]");
            Console.WriteLine();
            Console.WriteLine("Valid options:");
            Console.WriteLine("  -trait \"name=value\"    : only run tests with matching name/value traits");
            Console.WriteLine("                         : if specified more than once, acts as an OR operation");
            Console.WriteLine("  -notrait \"name=value\"  : do not run tests with matching name/value traits");
            Console.WriteLine("                         : if specified more than once, acts as an AND operation");
            Console.WriteLine();
        }

        static IEnumerable<string> ExpandFilePath(string path)
        {
            if (File.Exists(path))
            {
                yield return path;
            }
            else if (Directory.Exists(path))
            {
                foreach (var file in Directory.EnumerateFiles(path, "*.dll"))
                    yield return file;
            }
        }

    }
}
