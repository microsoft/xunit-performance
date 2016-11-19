//
// Derived from https://github.com/xunit/samples.xunit/blob/3f55e554e7de7eb2cd9802fa9e73a706520646cd/TestRunner/Program.cs.
//

using System;
using System.Threading;
using System.Reflection;
using Xunit.Runners;

namespace Microsoft.Xunit.Performance.Api
{
    public class XunitRunner
    {
        // We use consoleLock because messages can arrive in parallel, so we want to make sure we get
        // consistent console output.
        private object _consoleLock = new object();

        // Use an event to know when we're done
        private ManualResetEvent _finished = new ManualResetEvent(false);

        // Start out assuming success; we'll set this to 1 if we get a failed test
        private int _result = 0;

        public int Run(string assemblyPath)
        {
            // Create a runner for the specifid assembly.
            using(AssemblyRunner runner = AssemblyRunner.WithoutAppDomain(assemblyPath))
            {
                // Setup callbacks.
                runner.OnDiscoveryComplete = OnDiscoveryComplete;
                runner.OnExecutionComplete = OnExecutionComplete;
                runner.OnTestStarting = OnTestStarting;
                runner.OnTestFailed = OnTestFailed;
                runner.OnTestSkipped = OnTestSkipped;

                Console.WriteLine("\nDiscovering performance tests in {0}.", assemblyPath);

                // Start running tests.
                runner.Start(null);

                // Wait for tests to complete.
                _finished.WaitOne();
                _finished.Dispose();

                // Wait for the assembly runner to go idle.
                ManualResetEvent waitHandle = new ManualResetEvent(false);
                while(runner.Status != AssemblyRunnerStatus.Idle)
                {
                    waitHandle.WaitOne(10);
                }

                // Return overall status.
                return _result;
            }
        }

        private void OnDiscoveryComplete(DiscoveryCompleteInfo info)
        {
            lock (_consoleLock)
            {
                Console.WriteLine($"Running {info.TestCasesToRun} of {info.TestCasesDiscovered} tests...");
            }
        }

        private void OnExecutionComplete(ExecutionCompleteInfo info)
        {
            lock (_consoleLock)
            {
                Console.WriteLine($"Finished: {info.TotalTests} tests in {Math.Round(info.ExecutionTime, 3)}s ({info.TestsFailed} failed, {info.TestsSkipped} skipped)");
            }

            _finished.Set();
        }

        private void OnTestStarting(TestStartingInfo info)
        {
            lock (_consoleLock)
            {
                Console.WriteLine($"Running: {info.TestDisplayName}");
            }
        }

        private void OnTestFailed(TestFailedInfo info)
        {
            lock (_consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[FAIL] {0}: {1}", info.TestDisplayName, info.ExceptionMessage);
                
                if (info.ExceptionStackTrace != null)
                {
                    Console.WriteLine(info.ExceptionStackTrace);
                }

                Console.ResetColor();
            }

            _result = 1;
        }

        private void OnTestSkipped(TestSkippedInfo info)
        {
            lock (_consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[SKIP] {0}: {1}", info.TestDisplayName, info.SkipReason);
                Console.ResetColor();
            }
        }
    }
}