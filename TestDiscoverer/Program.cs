using System;
using Xunit.Abstractions;

namespace Xunit.TestDiscoverer
{
    /// <summary>
    /// Finds (but does not run) all XUnit tests in the given assembly.
    /// </summary>
    internal class Program
    {
        internal static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                PrintUsage();
                return;
            }

            // TODO: Other command-line options (e.g. output format, include/exclude traits)
            // TODO: If args[0] is a directory name instead of a filename or contains wildcards, then search for all applicable assemblies.
            DiscoverTests(args[0], TestCaseDiscovered);
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
            // TODO: More filtering (e.g. include/exclude traits)
            return testCase.SkipReason == null;
        }

        private static void TestCaseDiscovered(ITestCase testCase)
        {
            if (AcceptFilter(testCase))
            {
                // Note: DisplayName can be overridden in the source, but it defaults to the fully-qualified
                // name of the test method.
                //Console.WriteLine(testCase.DisplayName);

                Console.WriteLine($"Class:{testCase.TestMethod.TestClass.Class.Name} Method:{testCase.TestMethod.Method.Name}");
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
            Console.WriteLine("TestDiscoverer <assemblyPath>");
        }
    }
}
