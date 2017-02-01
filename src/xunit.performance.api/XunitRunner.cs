//
// Derived from https://github.com/xunit/samples.xunit/blob/3f55e554e7de7eb2cd9802fa9e73a706520646cd/TestRunner/Program.cs.
//

using System;
using System.Threading;
using Xunit.Runners;
using static Microsoft.Xunit.Performance.Api.XunitPerformanceLogger;

namespace Microsoft.Xunit.Performance.Api
{
    public static class XunitRunner
    {
        public static int Run(string assemblyPath, string typeName = null)
        {
            if(typeName != null && string.IsNullOrWhiteSpace(typeName))
                throw new ArgumentException("The test type name cannot be white space.");

            // Create a runner for the specifid assembly.
            using (var runner = AssemblyRunner.WithoutAppDomain(assemblyPath))
            {
                // Start out assuming success; we'll set this to 1 if we get a failed test
                var result = new int[] { 0 };

                // Use an event to know when we're done
                using (var finished = new ManualResetEvent(false))
                {
                    // We use consoleLock because messages can arrive in parallel, so we want to make sure we get
                    // consistent console output.
                    var consoleLock = new object();

                    SetupRunnerCallbacks(runner, finished, consoleLock, result);
                    WriteInfoLine($"Discovering performance tests in \"{assemblyPath}\".");

                    runner.Start(typeName);

                    // Wait for tests to complete.
                    finished.WaitOne();
                    finished.Dispose();
                }

                // Wait for the assembly runner to go idle.
                using (var waitHandle = new ManualResetEvent(false))
                    while (runner.Status != AssemblyRunnerStatus.Idle)
                        waitHandle.WaitOne(10);

                // Return overall status.
                return result[0];
            }
        }

        private static void SetupRunnerCallbacks(AssemblyRunner runner, ManualResetEvent manualResetEvent, object consoleLock, int[] result)
        {
            runner.OnDiscoveryComplete = info =>
            {
                lock (consoleLock)
                {
                    WriteInfoLine($"Running {info.TestCasesToRun} of {info.TestCasesDiscovered} tests...");
                }
            };
            runner.OnExecutionComplete = info =>
            {
                lock (consoleLock)
                {
                    WriteInfoLine($"Finished: {info.TotalTests} tests in {Math.Round(info.ExecutionTime, 3)}s ({info.TestsFailed} failed, {info.TestsSkipped} skipped)");
                }

                manualResetEvent.Set();
            };
            runner.OnTestStarting = info =>
            {
                lock (consoleLock)
                {
                    WriteInfoLine($"Running: {info.TestDisplayName}");
                }
            };
            runner.OnTestFailed = info =>
            {
                lock (consoleLock)
                {
                    WriteErrorLine($"{info.TestDisplayName}: {info.ExceptionMessage}");

                    if (info.ExceptionStackTrace != null)
                    {
                        WriteErrorLine(info.ExceptionStackTrace);
                    }
                }
                result[0] = 1;
            };
            runner.OnTestSkipped = info =>
            {
                lock (consoleLock)
                {
                    WriteWarningLine($"[SKIP] {info.TestDisplayName}: {info.SkipReason}");
                }
            };
        }
    }
}