using MathNet.Numerics.Statistics;
using Microsoft.Xunit.Performance.Analysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        protected override object CallTestMethod(object testClassInstance)
        {
            if (_loggingFailed)
                throw new Exception("ETW logging was requested, but this process is not running with elevated permissions.");

            return IterateAsync(testClassInstance);
        }

        private async Task IterateAsync(object testClassInstance)
        {
            var benchmarkTestCase = (BenchmarkTestCase)TestCase;
            var invoker = MakeInvokerDelegate(testClassInstance);
            var asyncSyncContext = (AsyncTestSyncContext)SynchronizationContext.Current;

            Stopwatch iterationTimer = new Stopwatch();
            Stopwatch overallTimer = new Stopwatch();
            RunningStatistics stats = new RunningStatistics();

            long totalMemoryAfterWarmup = 0;
            int gcCountAfterWarmup = 0;

            for (int i = 0; ; i++)
            {
                double elapsedMilliseconds = 0;

                if (i != 0 || !benchmarkTestCase.SkipWarmup)
                {
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
                }

                if (i == 0)
                {
                    totalMemoryAfterWarmup = GC.GetTotalMemory(true);
                    gcCountAfterWarmup = GC.CollectionCount(0);
                    overallTimer.Start();
                }
                else
                {
                    stats.Push(elapsedMilliseconds);

                    //
                    // Keep running iterations until we've reached the desired margin of error in the result.
                    //
                    if (stats.MarginOfError(benchmarkTestCase.Confidence) < benchmarkTestCase.MarginOfError)
                    {
                        //
                        // If the test says it doesn't use the GC, we can stop now.
                        //
                        if (benchmarkTestCase.TriggersGC.HasValue && !benchmarkTestCase.TriggersGC.Value)
                            break;

                        //
                        // If a GC ocurred during the iterations so far, then we've accounted for that in the result,
                        // and can stop now.
                        //
                        if (GC.CollectionCount(0) > gcCountAfterWarmup)
                            break;

                        //
                        // If the test has not stated whether it uses the GC, we need to guess.
                        //
                        if (!benchmarkTestCase.TriggersGC.HasValue)
                        {
                            //
                            // Maybe the method allocates, but we haven't executed enough iterations for this to trigger
                            // the GC.  If so, we're missing a large part of the cost of the method.  But, some methods will
                            // never allocate, and so will never trigger a GC.  So we need to give up if it looks like nobody
                            // is allocating anything.
                            //
                            // (We can't *just* check GC.GetTotalMemory, because it's only updated when each thread's 
                            // "allocation context" is exhausted.  So we make sure to run for a while before trusting
                            // GC.GetTotalMemory.)
                            //
                            if (i >= 1024 && GC.GetTotalMemory(false) == totalMemoryAfterWarmup)
                                break;

                            //
                            // If the iterations so far have taken a significant amount of time, and yet a GC has not occurred,
                            // we give up and assume that the GC isn't going to be a significant factor for this method.
                            //
                            if (overallTimer.Elapsed.TotalSeconds >= 1)
                                break;
                        }
                    }
                }
            }

        }


        private Func<object> MakeInvoker0()
        {
            if (TestMethod.ReturnType == typeof(void))
            {
                var action = (Action)TestMethod.CreateDelegate(typeof(Action));
                return () => { action(); return null; };
            }
            else
            {
                return (Func<object>)TestMethod.CreateDelegate(typeof(Func<object>));
            }
        }

        private Func<object> MakeInvoker1<T>(T arg)
        {
            if (TestMethod.ReturnType == typeof(void))
            {
                var action = (Action<T>)TestMethod.CreateDelegate(typeof(Action<T>));
                return () => { action(arg); return null; };
            }
            else
            {
                var func = (Func<T, object>)TestMethod.CreateDelegate(typeof(Func<T, object>));
                return () => func(arg);
            }
        }

        private Func<object> MakeInvoker2<T1, T2>(T1 arg1, T2 arg2)
        {
            if (TestMethod.ReturnType == typeof(void))
            {
                var action = (Action<T1, T2>)TestMethod.CreateDelegate(typeof(Action<T1, T2>));
                return () => { action(arg1, arg2); return null; };
            }
            else
            {
                var func = (Func<T1, T2, object>)TestMethod.CreateDelegate(typeof(Func<T1, T2, object>));
                return () => func(arg1, arg2);
            }
        }

        private Func<object> MakeInvokerDelegate(object testClassInstance)
        {
            object[] args;
            Type[] types;
            var testMethodParamTypes = TestMethod.GetParameters().Select(p => p.ParameterType).ToArray();

            if (testClassInstance == null)
            {
                args = TestMethodArguments;
                types = testMethodParamTypes;
            }
            else
            {
                args = new object[TestMethodArguments.Length + 1];
                types = new Type[TestMethodArguments.Length + 1];
                args[0] = testClassInstance;
                types[0] = testClassInstance.GetType();
                Array.Copy(TestMethodArguments, 0, args, 1, TestMethodArguments.Length);
                Array.Copy(testMethodParamTypes, 0, types, 1, testMethodParamTypes.Length);
            }

            if (args.Length == 0)
                return MakeInvoker0();

            string invokerFactoryName;

            switch (args.Length)
            {
                case 1: invokerFactoryName = nameof(MakeInvoker1); break;
                case 2: invokerFactoryName = nameof(MakeInvoker2); break;

                default: return () => TestMethod.Invoke(testClassInstance, TestMethodArguments);
            }

            var invokerFactory = typeof(BenchmarkTestInvoker).GetMethod(invokerFactoryName, BindingFlags.Instance | BindingFlags.NonPublic);
            var invokerFactoryInstance = invokerFactory.MakeGenericMethod(types);

            return (Func<object>)invokerFactoryInstance.Invoke(this, args);
        }


        /// <summary>
        /// Computes whether we've executed enough iterations to have the desired margin of error, with the desired confidence.
        /// </summary>
        /// <param name="stats"></param>
        /// <param name="marginOfError"></param>
        /// <param name="confidence"></param>
        /// <returns></returns>
        private bool HaveDesiredMarginOfError(RunningStatistics stats, double marginOfError, double confidence)
        {
            if (stats.Count < 2)
                return false;

            var stderr = stats.StandardDeviation / Math.Sqrt(stats.Count);
            var t = MathNet.Numerics.ExcelFunctions.TInv(1.0 - confidence, (int)stats.Count - 1);
            var mean = stats.Mean;
            var interval = t * stderr;

            return (interval / mean) < marginOfError;
        }
    }
}
