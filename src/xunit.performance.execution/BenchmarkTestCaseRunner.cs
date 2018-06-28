// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Xunit.Performance
{
    class BenchmarkTestCaseRunner : XunitTestCaseRunner
    {
        readonly ExceptionAggregator _cleanupAggregator = new ExceptionAggregator();
        readonly IMessageSink _diagnosticMessageSink;
        readonly bool _discoverArguments;
        readonly List<BenchmarkTestRunner> _testRunners = new List<BenchmarkTestRunner>();
        readonly List<IDisposable> _toDispose = new List<IDisposable>();
        Exception _dataDiscoveryException;

        public BenchmarkTestCaseRunner(IXunitTestCase testCase,
                                         string displayName,
                                         string skipReason,
                                         object[] constructorArguments,
                                         object[] testMethodArguments,
                                         IMessageSink diagnosticMessageSink,
                                         IMessageBus messageBus,
                                         ExceptionAggregator aggregator,
                                         CancellationTokenSource cancellationTokenSource)
            : base(testCase, displayName, skipReason, constructorArguments, testMethodArguments, messageBus, aggregator, cancellationTokenSource)
        {
            _diagnosticMessageSink = diagnosticMessageSink;
            _discoverArguments = testMethodArguments == null;
        }

        public static Exception Unwrap(Exception ex)
        {
            while (true)
            {
                if (!(ex is TargetInvocationException tiex))
                    return ex;

                ex = tiex.InnerException;
            }
        }

        protected override async Task AfterTestCaseStartingAsync()
        {
            await base.AfterTestCaseStartingAsync();

            if (_discoverArguments)
            {
                try
                {
                    var dataAttributes = TestCase.TestMethod.Method.GetCustomAttributes(typeof(DataAttribute));

                    foreach (var dataAttribute in dataAttributes)
                    {
                        var discovererAttribute = dataAttribute.GetCustomAttributes(typeof(DataDiscovererAttribute)).First();
                        var args = discovererAttribute.GetConstructorArguments().Cast<string>().ToList();
                        var discoverer = ExtensibilityPointFactory.GetDataDiscoverer(_diagnosticMessageSink, discovererAttribute);

                        foreach (var dataRow in discoverer.GetData(dataAttribute, TestCase.TestMethod.Method))
                        {
                            _toDispose.AddRange(dataRow.OfType<IDisposable>());

                            ITypeInfo[] resolvedTypes = null;
                            var methodToRun = TestMethod;

                            if (methodToRun.IsGenericMethodDefinition)
                            {
                                resolvedTypes = TestCase.TestMethod.Method.ResolveGenericTypes(dataRow);
                                methodToRun = methodToRun.MakeGenericMethod(resolvedTypes.Select(t => ((IReflectionTypeInfo)t).Type).ToArray());
                            }

                            var parameterTypes = methodToRun.GetParameters().Select(p => p.ParameterType).ToArray();
                            var convertedDataRow = Reflector.ConvertArguments(dataRow, parameterTypes);
                            var theoryDisplayName = TestCase.TestMethod.Method.GetDisplayNameWithArguments(DisplayName, convertedDataRow, resolvedTypes);
                            var test = new XunitTest(TestCase, theoryDisplayName);

                            _testRunners.Add(new BenchmarkTestRunner(test, MessageBus, TestClass, ConstructorArguments, methodToRun, convertedDataRow, SkipReason, BeforeAfterAttributes, Aggregator, CancellationTokenSource));
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Stash the exception so we can surface it during RunTestAsync
                    _dataDiscoveryException = ex;
                }
            }

            if (_testRunners.Count == 0)
            {
                var test = new XunitTest(TestCase, DisplayName);
                _testRunners.Add(new BenchmarkTestRunner(test, MessageBus, TestClass, ConstructorArguments, TestMethod, TestMethodArguments, SkipReason, BeforeAfterAttributes, Aggregator, CancellationTokenSource));
            }
        }

        protected override Task BeforeTestCaseFinishedAsync()
        {
            Aggregator.Aggregate(_cleanupAggregator);

            return base.BeforeTestCaseFinishedAsync();
        }

        protected override async Task<RunSummary> RunTestAsync()
        {
            if (_dataDiscoveryException != null)
                return RunTest_DataDiscoveryException();

            var runSummary = new RunSummary();

            foreach (var testRunner in _testRunners)
                runSummary.Aggregate(await testRunner.RunAsync());

            // Run the cleanup here so we can include cleanup time in the run summary,
            // but save any exceptions so we can surface them during the cleanup phase,
            // so they get properly reported as test case cleanup failures.
            var timer = new ExecutionTimer();
            foreach (var disposable in _toDispose)
                timer.Aggregate(() => _cleanupAggregator.Run(disposable.Dispose));

            runSummary.Time += timer.Total;

            return runSummary;
        }

        RunSummary RunTest_DataDiscoveryException()
        {
            var test = new XunitTest(TestCase, DisplayName);

            if (!MessageBus.QueueMessage(new TestStarting(test)))
                CancellationTokenSource.Cancel();
            else if (!MessageBus.QueueMessage(new TestFailed(test, 0, null, Unwrap(_dataDiscoveryException))))
                CancellationTokenSource.Cancel();
            if (!MessageBus.QueueMessage(new TestFinished(test, 0, null)))
                CancellationTokenSource.Cancel();

            return new RunSummary { Total = 1, Failed = 1 };
        }
    }
}