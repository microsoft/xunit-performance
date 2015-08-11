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
        static IDisposable _etwLogger; // just to keep the logger rooted, so it doesn't get finalized during the run
        static SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        
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
                    if (ETWLogging.CanLog)
                        _etwLogger = ETWLogging.Start(BenchmarkConfiguration.ETLPath, BenchmarkConfiguration.RunId);

                    _initialized = true;
                }
            }
        }

        protected override async Task<decimal> InvokeTestMethodAsync(object testClassInstance)
        {
            //
            // Serialize all benchmarks
            //
            await _semaphore.WaitAsync();
            try
            {
                return await base.InvokeTestMethodAsync(testClassInstance);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        protected override object CallTestMethod(object testClassInstance)
        {
            return IterateAsync(testClassInstance);
        }

        private async Task IterateAsync(object testClassInstance)
        {
            var asyncSyncContext = (AsyncTestSyncContext)SynchronizationContext.Current;
            string stopReason = "Unknown";

            try
            {
                BenchmarkEventSource.Log.BenchmarkStart(BenchmarkConfiguration.RunId, DisplayName);

                var benchmarkTestCase = (BenchmarkTestCase)TestCase;
                var invoker = MakeInvokerDelegate(testClassInstance);

                Stopwatch iterationTimer = new Stopwatch();
                Stopwatch overallTimer = new Stopwatch();

                var allocatesAttribute = TestCase.TestMethod.Method.GetCustomAttributes(typeof(AllocatesAttribute)).FirstOrDefault();
                var allocates = (bool?)allocatesAttribute?.GetConstructorArguments().First();

                for (int i = 0; ; i++)
                {
                    double elapsedMilliseconds = 0;

                    if (i != 0 || !benchmarkTestCase.SkipWarmup)
                    {
                        if (!allocates.HasValue || allocates.Value)
                        {
                            GC.Collect(2, GCCollectionMode.Optimized);
                            GC.WaitForPendingFinalizers();
                        }

                        bool success = false;
                        BenchmarkEventSource.Log.BenchmarkIterationStart(BenchmarkConfiguration.RunId, DisplayName, i);
                        iterationTimer.Restart();

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
                            BenchmarkEventSource.Log.BenchmarkIterationStop(BenchmarkConfiguration.RunId, DisplayName, i, success);

                            if (!success)
                                stopReason = "TestFailed";
                        }

                        if (!success)
                            break;
                    }

                    if (i == 0)
                    {
                        overallTimer.Start();
                    }
                    else if (overallTimer.ElapsedMilliseconds >= BenchmarkConfiguration.MaxTotalMilliseconds)
                    {
                        stopReason = "MaxTime";
                        break;
                    }
                    else if (i >= BenchmarkConfiguration.MaxIteration)
                    {
                        stopReason = "MaxIterations";
                        break;
                    }
                }
            }
            finally
            {
                BenchmarkEventSource.Log.BenchmarkStop(BenchmarkConfiguration.RunId, DisplayName, stopReason);
            }
        }


        /// <summary>
        /// Make a delegate that invokes the test method with this case's arguments.  The idea is that invoking the delegate
        /// will be faster than using MethodInfo.Invoke, which may matter if we're running a lot of small iterations of the method.
        /// </summary>
        /// <param name="testClassInstance"></param>
        /// <returns></returns>
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
                case 3: invokerFactoryName = nameof(MakeInvoker3); break;
                case 4: invokerFactoryName = nameof(MakeInvoker4); break;

                default: return () => TestMethod.Invoke(testClassInstance, TestMethodArguments);
            }

            var invokerFactory = typeof(BenchmarkTestInvoker).GetMethod(invokerFactoryName, BindingFlags.Instance | BindingFlags.NonPublic);
            var invokerFactoryInstance = invokerFactory.MakeGenericMethod(types);

            return (Func<object>)invokerFactoryInstance.Invoke(this, args);
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

        private Func<object> MakeInvoker3<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
        {
            if (TestMethod.ReturnType == typeof(void))
            {
                var action = (Action<T1, T2, T3>)TestMethod.CreateDelegate(typeof(Action<T1, T2, T3>));
                return () => { action(arg1, arg2, arg3); return null; };
            }
            else
            {
                var func = (Func<T1, T2, T3, object>)TestMethod.CreateDelegate(typeof(Func<T1, T2, T3, object>));
                return () => func(arg1, arg2, arg3);
            }
        }

        private Func<object> MakeInvoker4<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (TestMethod.ReturnType == typeof(void))
            {
                var action = (Action<T1, T2, T3, T4>)TestMethod.CreateDelegate(typeof(Action<T1, T2, T3, T4>));
                return () => { action(arg1, arg2, arg3, arg4); return null; };
            }
            else
            {
                var func = (Func<T1, T2, T3, T4, object>)TestMethod.CreateDelegate(typeof(Func<T1, T2, T3, T4, object>));
                return () => func(arg1, arg2, arg3, arg4);
            }
        }
    }
}
