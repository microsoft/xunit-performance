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
    sealed class BenchmarkTestInvoker : XunitTestInvoker
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
            var benchmarkAttr = (BenchmarkAttribute)TestMethod.GetCustomAttribute(typeof(BenchmarkAttribute));
            var iterator = new BenchmarkIteratorImpl(DisplayName, benchmarkAttr.InnerIterationCount);
            return iterator.RunAsync(async () =>
            {
                var success = false;
                BenchmarkEventSource.Log.BenchmarkStart(BenchmarkConfiguration.Instance.RunId, DisplayName);
                try
                {
                    var result = TestMethod.Invoke(testClassInstance, TestMethodArguments);

                    if (result is Task task)
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

                    if (iterator.IterationStopReason == "NoIterations")
                    {
                        success = false;
                        throw new Exception("Benchmark did not execute any iterations.  Please use one of the iteration methods in Microsoft.Xunit.Performance.Benchmark");
                    }
                }
                finally
                {
                    var stopReason = success ? iterator.IterationStopReason : "TestFailed";
                    BenchmarkEventSource.Log.BenchmarkStop(BenchmarkConfiguration.Instance.RunId, DisplayName, stopReason);
                    BenchmarkEventSource.Log.Flush();
                }
            });
        }

        internal sealed class BenchmarkIteratorImpl : BenchmarkIterator
        {
            static Random _randomDelayGenerator;
            static int _randomDelaySpinLimit;
            readonly int _maxIterations;
            readonly Stopwatch _overallTimer;
            readonly string _testName;
            int _currentIteration;
            bool _currentIterationMeasurementStarted;
            bool _currentIterationMeasurementStopped;
            bool _startedIteration;

            public BenchmarkIteratorImpl(string testName)
                : this(testName, 1)
            {
            }

            public BenchmarkIteratorImpl(string testName, long innerIterationsCount)
                : base(innerIterationsCount)
            {
                _testName = testName;
                _overallTimer = new Stopwatch();
                _currentIteration = -1;

                _maxIterations = (InnerIterationCount > 1) ?
                    BenchmarkConfiguration.Instance.MaxIterationWhenInnerSpecified :
                    BenchmarkConfiguration.Instance.MaxIteration;

                IterationStopReason = "NoIterations";
            }

            internal string IterationStopReason { get; private set; }

            protected internal override IEnumerable<BenchmarkIteration> Iterations
            {
                get
                {
                    if (_startedIteration)
                        throw new InvalidOperationException("Cannot use Benchmark.Iterations twice in a single test method.");
                    _startedIteration = true;
                    return GetIterations();
                }
            }

            bool DoneIterating
            {
                get
                {
                    if (_currentIteration == 0)
                        return false;

                    if (_currentIteration > _maxIterations)
                    {
                        IterationStopReason = "MaxIterations";
                        return true;
                    }

                    if (_currentIteration > BenchmarkConfiguration.Instance.MinIteration &&
                        _overallTimer.ElapsedMilliseconds > BenchmarkConfiguration.Instance.MaxTotalMilliseconds)
                    {
                        IterationStopReason = "MaxTime";
                        return true;
                    }

                    return false;
                }
            }

            protected internal override void StartMeasurement(int iterationNumber)
            {
                if (iterationNumber == _currentIteration)
                {
                    if (_currentIterationMeasurementStarted)
                        throw new InvalidOperationException("StartMeasurement already called for the current iteration");

                    _currentIterationMeasurementStarted = true;

                    RandomizeMeasurementStartTime();

                    BenchmarkEventSource.Log.BenchmarkIterationStart(BenchmarkConfiguration.Instance.RunId, _testName, iterationNumber);
                }
            }

            protected internal override void StopMeasurement(int iterationNumber)
            {
                if (iterationNumber == _currentIteration && !_currentIterationMeasurementStopped)
                {
                    Debug.Assert(_currentIterationMeasurementStarted);
                    _currentIterationMeasurementStopped = true;

                    BenchmarkEventSource.Log.BenchmarkIterationStop(BenchmarkConfiguration.Instance.RunId, _testName, iterationNumber);
                }
            }

            //
            // Insert a small random delay, to ensure that any noise introduced due to system timer resolution has a nice random distribution.
            //
            static void RandomizeMeasurementStartTime()
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

            static void SpinDelay(int spinCount)
            {
                // Spin for a bit.  The volatile read is just to make sure the compiler doesn't optimize away this loop.
                for (int i = 0; i < spinCount; i++)
                    Volatile.Read(ref _randomDelayGenerator);
            }

            IEnumerable<BenchmarkIteration> GetIterations()
            {
                GC.Collect(2, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();
                GC.Collect(2, GCCollectionMode.Forced);

                for (_currentIteration = 0; !DoneIterating; _currentIteration++)
                {
                    _currentIterationMeasurementStarted = false;
                    _currentIterationMeasurementStopped = false;

                    yield return CreateIteration(_currentIteration);

                    if (!_currentIterationMeasurementStarted)
                        throw new Exception("Test iteration was not measured.  Use Microsoft.Xunit.Performance.BenchmarkIteration.StartMeasurement in each iteration.");

                    StopMeasurement(_currentIteration);

                    if (_currentIteration == 0)
                    {
                        _overallTimer.Start();
                    }
                }
            }
        }
    }
}