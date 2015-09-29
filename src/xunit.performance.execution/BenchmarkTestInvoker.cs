// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance.Internal;
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
        }

        protected override object CallTestMethod(object testClassInstance)
        {
            var asyncSyncContext = (AsyncTestSyncContext)SynchronizationContext.Current;

            //
            // Run the test method inside of our iterator.  Note that BenchmarkIterator.Run ensures that only one test
            // method is running at any given time, so we don't need extra synchronization here.
            //
            var iterator = new BenchmarkIteratorImpl(DisplayName);
            return iterator.RunAsync(async () =>
            {
                var success = false;
                BenchmarkEventSource.Log.BenchmarkStart(BenchmarkConfiguration.RunId, DisplayName);
                try
                {
                    var result = TestMethod.Invoke(testClassInstance, TestMethodArguments);

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
                    var stopReason = success ? iterator.IterationStopReason : "TestFailed";
                    BenchmarkEventSource.Log.BenchmarkStop(BenchmarkConfiguration.RunId, DisplayName, stopReason);
                    BenchmarkEventSource.Log.Flush();
                }
            });
        }

        internal class BenchmarkIteratorImpl : BenchmarkIterator
        {
            private readonly string _testName;
            private readonly Stopwatch _overallTimer;
            private bool _startedIteration;
            private int _currentIteration;
            private bool _currentIterationMeasurementStarted;
            private bool _currentIterationMesaurementStopped;

            internal string IterationStopReason { get; private set; }

            public BenchmarkIteratorImpl(string testName)
            {
                _testName = testName;
                _overallTimer = new Stopwatch();
                _currentIteration = -1;
            }

            private bool DoneIterating
            {
                get
                {
                    if (_currentIteration == 0)
                        return false;

                    if (_currentIteration > BenchmarkConfiguration.MaxIteration)
                    {
                        IterationStopReason = "MaxIterations";
                        return true;
                    }

                    if (_currentIteration > 1 && _overallTimer.ElapsedMilliseconds > BenchmarkConfiguration.MaxTotalMilliseconds)
                    {
                        IterationStopReason = "MaxTime";
                        return true;
                    }

                    return false;
                }
            }

            protected override IEnumerable<BenchmarkIteration> Iterations
            {
                get
                {
                    if (_startedIteration)
                        throw new InvalidOperationException("Cannot use Benchmark.Iterations twice in a single test method.");
                    _startedIteration = true;
                    return GetIterations();
                }
            }

            private IEnumerable<BenchmarkIteration> GetIterations()
            {
                for (_currentIteration = 0; !DoneIterating; _currentIteration++)
                {
                    _currentIterationMeasurementStarted = false;
                    _currentIterationMesaurementStopped = false;

                    yield return CreateIteration(_currentIteration);

                    if (_currentIterationMeasurementStarted)
                        StopMeasurement(_currentIteration);

                    if (_currentIteration == 0)
                        _overallTimer.Start();
                }
            }

            protected override void StartMeasurement(int iterationNumber)
            {
                if (iterationNumber == _currentIteration)
                {
                    if (_currentIterationMeasurementStarted)
                        throw new InvalidOperationException("StartMeasurement already called for the current iteration");

                    _currentIterationMeasurementStarted = true;

                    GC.Collect(2, GCCollectionMode.Optimized);
                    GC.WaitForPendingFinalizers();

                    RandomizeMeasurementStartTime();

                    BenchmarkEventSource.Log.BenchmarkIterationStart(BenchmarkConfiguration.RunId, _testName, iterationNumber);
                }
            }

            static Random _randomDelayGenerator;
            static int _randomDelaySpinLimit;

            //
            // Insert a small random delay, to ensure that any noise introduced due to system timer resolution has a nice random distribution.
            //
            private static void RandomizeMeasurementStartTime()
            {
                int spinCount;

                lock (typeof(BenchmarkIteratorImpl))
                {
                    //
                    // The first time we run this, we need to create a random number generator, and find a spin limit 
                    // corresponding to a significant number of Stopwatch ticks.
                    //
                    if (_randomDelayGenerator == null)
                    {
                        _randomDelayGenerator = new Random();

                        // Very short spin, just to "warm up" the spin loop.
                        SpinDelay(10);

                        for (int calibrationSpinCount = 1024; ; calibrationSpinCount *= 2)
                        {
                            var start = Stopwatch.GetTimestamp();
                            SpinDelay(calibrationSpinCount);
                            var elapsed = Stopwatch.GetTimestamp() - start;

                            if (elapsed >= 1000)
                            {
                                // Set the limit at roughly 100 Stopwatch ticks.
                                _randomDelaySpinLimit = (int)((100 * calibrationSpinCount) / elapsed);
                                break;
                            }
                        }
                    }

                    spinCount = _randomDelayGenerator.Next(_randomDelaySpinLimit);
                }

                SpinDelay(spinCount);
            }

            private static void SpinDelay(int spinCount)
            {
                // Spin for a bit.  The volatile read is just to make sure the compiler doesn't optimize away this loop.
                for (int i = 0; i < spinCount; i++)
                    Volatile.Read(ref _randomDelayGenerator);
            }

            protected override void StopMeasurement(int iterationNumber)
            {
                if (iterationNumber == _currentIteration && !_currentIterationMesaurementStopped)
                {
                    Debug.Assert(_currentIterationMeasurementStarted);
                    _currentIterationMesaurementStopped = true;

                    // TODO: we should remove the "Success" parameter; this is already communicated elsewhere, and the information isn't
                    // easily available here.
                    BenchmarkEventSource.Log.BenchmarkIterationStop(BenchmarkConfiguration.RunId, _testName, iterationNumber, Success: true);
                }
            }
        }
    }
}
