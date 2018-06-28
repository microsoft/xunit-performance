//
// Derived from https://github.com/xunit/samples.xunit/blob/3f55e554e7de7eb2cd9802fa9e73a706520646cd/TestRunner/Program.cs.
//

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Xunit.Abstractions;
using Xunit.Runners;
using static Microsoft.Xunit.Performance.Api.PerformanceLogger;

namespace Microsoft.Xunit.Performance.Api
{
    static class XunitRunner
    {
        public static int Run(string assemblyPath, List<string> typeNames = null)
        {
            // Create a runner for the specifid assembly.
            using (AssemblyRunner runner = AssemblyRunner.WithoutAppDomain(assemblyPath))
            {
                // Start out assuming success; we'll set this to 1 if we get a failed test
                var result = new int[] { 0 };

                if (typeNames?.Count == 0)
                {
                    Run(runner, result, null);
                }
                else
                {
                    typeNames.ForEach(typeName => Run(runner, result, typeName));
                }

                // Wait for the assembly runner to go idle.
                using (var waitHandle = new ManualResetEvent(false))
                {
                    while (runner.Status != AssemblyRunnerStatus.Idle)
                    {
                        waitHandle.WaitOne(10);
                    }
                }

                // Return overall status.
                return result[0];
            }
        }

        static void Run(AssemblyRunner runner, int[] result, string typeName)
        {
            if (typeName != null && string.IsNullOrWhiteSpace(typeName))
                throw new ArgumentException("The test type name cannot be white space.");

            // Use an event to know when we're done
            using (var finished = new ManualResetEvent(false))
            {
                // We use consoleLock because messages can arrive in parallel, so we want to make sure we get
                // consistent console output.
                var consoleLock = new object();

                SetupRunnerCallbacks(runner, finished, consoleLock, result);
                runner.Start(typeName);
                finished.WaitOne(); // Wait for tests to complete.
            }
        }

        static void SetupRunnerCallbacks(AssemblyRunner runner, ManualResetEvent manualResetEvent, object consoleLock, int[] result)
        {
            runner.TestCaseFilter = (ITestCase testCase) =>
            {
                return testCase.SkipReason == null && testCase.GetType() == typeof(BenchmarkTestCase);
            };

            runner.OnDiscoveryComplete = info =>
            {
                lock (consoleLock)
                {
                    var diff = info.TestCasesDiscovered - info.TestCasesToRun;
                    if (diff < 0) // This should never happen.
                    {
                        // TODO: Add error handling. Throwing exception is not enough because we are waiting for the async runner to finish.
                        WriteErrorLine("There are more test cases to run than test cases discovered.");
                    }
                    WriteInfoLine($"Running {info.TestCasesToRun} [Benchmark]s");
                    if (diff != 0)
                        WriteWarningLine($"Skipping {diff} microbenchmarks.");
                }
            };
            runner.OnErrorMessage = info =>
            {
                lock (consoleLock)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Got an unhandled exception outside test.");
                    sb.AppendLine($"  {info.ExceptionType}: {info.MesssageType}: {info.ExceptionMessage}");
                    sb.AppendLine($"  {info.ExceptionStackTrace}");
                    WriteErrorLine(sb.ToString());
                }
            };
            runner.OnExecutionComplete = info =>
            {
                lock (consoleLock)
                {
                    WriteInfoLine($"Finished {info.TotalTests} tests in {Math.Round(info.ExecutionTime, 3)}s ({info.TestsFailed} failed, {info.TestsSkipped} skipped)");
                }

                manualResetEvent.Set();
            };
            runner.OnTestStarting = info =>
            {
                lock (consoleLock)
                {
                    WriteInfoLine($"  {info.TestDisplayName}");
                }
            };
            runner.OnTestFailed = info =>
            {
                lock (consoleLock)
                {
                    // TODO: Stop reporting performance results of failed test!
                    WriteErrorLine($"{info.TestDisplayName}: {info.ExceptionMessage}");

                    if (info.ExceptionStackTrace != null)
                    {
                        WriteErrorLine(info.ExceptionStackTrace);
                    }
                }
                result[0] += 1;
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