using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Xunit.Performance
{
    internal class BenchmarkTestInvoker : XunitTestInvoker
    {
        static bool _initialized;
        static bool _loggingFailed;
        static string _runId;
        static IDisposable _etwLogger; // just to keep the logger rooted, so it doesn't get finalized during the run

        public BenchmarkTestInvoker(ITest test,
                                IMessageBus messageBus,
                                Type testClass,
                                object[] constructorArguments,
                                MethodInfo testMethod,
                                object[] testMethodArguments,
                                IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes,
                                ExceptionAggregator aggregator,
                                CancellationTokenSource cancellationTokenSource)
            : base(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, beforeAfterAttributes, aggregator, cancellationTokenSource)
        {
            lock(typeof(BenchmarkTestInvoker))
            {
                if (!_initialized)
                {
                    _runId = Environment.GetEnvironmentVariable("XUNIT_PERFORMANCE_RUN_ID");
                    if (string.IsNullOrEmpty(_runId))
                        _runId = Environment.MachineName + ":" + DateTimeOffset.UtcNow.ToString("u");

                    var etwLogPath = Environment.GetEnvironmentVariable("XUNIT_PERFORMANCE_ETL_PATH");
                    if (etwLogPath == null)
                        etwLogPath = "xunit.performance.etl";
                    else if (!ETWLogging.CanLog)
                        _loggingFailed = true;

                    if (ETWLogging.CanLog)
                        _etwLogger = ETWLogging.Start(etwLogPath, _runId);

                    _initialized = true;
                }
            }
        }

        protected override async Task<decimal> InvokeTestMethodAsync(object testClassInstance)
        {
            var oldSyncContext = SynchronizationContext.Current;
            try
            {
                var asyncSyncContext = new AsyncTestSyncContext(oldSyncContext);
                SetSynchronizationContext(asyncSyncContext);

                await Aggregator.RunAsync(
                    () => Timer.AggregateAsync(
                        async () =>
                        {
                            var parameterCount = TestMethod.GetParameters().Length;
                            var valueCount = TestMethodArguments == null ? 0 : TestMethodArguments.Length;
                            if (parameterCount != valueCount)
                            {
                                Aggregator.Add(
                                    new InvalidOperationException(
                                        string.Format("The test method expected {0} parameter value{1}, but {2} parameter value{3} {4} provided.",
                                                      parameterCount, parameterCount == 1 ? "" : "s",
                                                      valueCount, valueCount == 1 ? "" : "s", valueCount == 1 ? "was" : "were"))
                                );
                            }
                            else if (_loggingFailed)
                            {
                                Aggregator.Add(
                                    new Exception("ETW logging was requested, but this process is not running with elevated permissions."));
                            }
                            else
                            {
                                var invoker = MakeInvokerDelegate(testClassInstance);

                                Stopwatch iterationTimer = new Stopwatch();
                                Stopwatch overallTimer = new Stopwatch();
                                RunningStatistics stats = new RunningStatistics();

                                long totalMemoryAfterWarmup = 0;
                                int gcCountAfterWarmup = 0;

                                for (int i = 0; ; i++)
                                {
                                    double elapsedMilliseconds;
                                    bool success = false;
                                    BenchmarkEventSource.Log.BenchmarkExecutionStart(_runId, DisplayName, i);
                                    iterationTimer.Start();

                                    try
                                    {
                                        object result = invoker();

                                        var task = result as Task;
                                        if (task != null)
                                        {
                                            await task;
                                            success = true;
                                        }
                                        else
                                        {
                                            var ex = await asyncSyncContext.WaitForCompletionAsync();
                                            if (ex == null)
                                                success = true;
                                            else
                                                Aggregator.Add(ex);
                                        }
                                    }
                                    finally
                                    {
                                        iterationTimer.Stop();
                                        elapsedMilliseconds = iterationTimer.Elapsed.TotalMilliseconds;
                                        BenchmarkEventSource.Log.BenchmarkExecutionStop(_runId, DisplayName, i, success);
                                    }

                                    if (!success)
                                        break;

                                    if (i == 0)
                                    {
                                        totalMemoryAfterWarmup = GC.GetTotalMemory(true);
                                        gcCountAfterWarmup = GC.CollectionCount(0);
                                        overallTimer.Start();
                                    }
                                    else
                                    {
                                        stats.Push(elapsedMilliseconds);

                                        if (EnoughIterations(stats, 0.05))
                                        {
                                            if (GC.CollectionCount(0) > gcCountAfterWarmup)
                                                break;

                                            if (i >= 1024 && GC.GetTotalMemory(false) == totalMemoryAfterWarmup)
                                                break;

                                            if (overallTimer.Elapsed.TotalMilliseconds >= 10)
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                    )
                );
            }
            finally
            {
                SetSynchronizationContext(oldSyncContext);
            }

            return Timer.Total;
        }

        private Func<object> MakeInvokerDelegate(object testClassInstance)
        {
            if (TestMethodArguments == null || TestMethodArguments.Length == 0)
            {
                if (TestMethod.ReturnType == typeof(void))
                {
                    Action testMethodAction = (Action)TestMethod.CreateDelegate(typeof(Action), testClassInstance);
                    return () => { testMethodAction(); return null; };
                }
                else if (typeof(Task).IsAssignableFrom(TestMethod.ReturnType))
                {
                    return (Func<Task>)TestMethod.CreateDelegate(typeof(Func<Task>), testClassInstance);
                }
            }

            return () => TestMethod.Invoke(testClassInstance, TestMethodArguments);
        }

        [System.Security.SecuritySafeCritical]
        static void SetSynchronizationContext(SynchronizationContext context)
        {
            SynchronizationContext.SetSynchronizationContext(context);
        }


        private bool EnoughIterations(RunningStatistics stats, double threshold)
        {
            if (stats.Count < 2)
                return false;

            if (stats.Count == 1000)
                Aggregator.Add(new Exception($"Test is extremely noisy"));

            var stderr = stats.StandardDeviation / Math.Sqrt(stats.Count);
            var t = MathNet.Numerics.ExcelFunctions.TInv(0.05, (int)stats.Count - 1);
            var mean = stats.Mean;
            var interval = t * stderr;
            var ratio = interval / mean;
            return ratio < threshold;
        }
    }
}
