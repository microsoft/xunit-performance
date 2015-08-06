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
    internal class BenchmarkTestCaseRunner : XunitTestCaseRunner
    {
        readonly ExceptionAggregator cleanupAggregator = new ExceptionAggregator();
        Exception dataDiscoveryException;
        readonly IMessageSink diagnosticMessageSink;
        readonly List<BenchmarkTestRunner> testRunners = new List<BenchmarkTestRunner>();
        readonly List<IDisposable> toDispose = new List<IDisposable>();
        readonly bool discoverArguments;


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
            this.diagnosticMessageSink = diagnosticMessageSink;
            this.discoverArguments = testMethodArguments == null;
        }

        protected override async Task AfterTestCaseStartingAsync()
        {
            await base.AfterTestCaseStartingAsync();

            if (discoverArguments)
            {
                try
                {
                    var dataAttributes = TestCase.TestMethod.Method.GetCustomAttributes(typeof(DataAttribute));

                    foreach (var dataAttribute in dataAttributes)
                    {
                        var discovererAttribute = dataAttribute.GetCustomAttributes(typeof(DataDiscovererAttribute)).First();
                        var args = discovererAttribute.GetConstructorArguments().Cast<string>().ToList();
                        var discoverer = ExtensibilityPointFactory.GetDataDiscoverer(diagnosticMessageSink, discovererAttribute);

                        foreach (var dataRow in discoverer.GetData(dataAttribute, TestCase.TestMethod.Method))
                        {
                            toDispose.AddRange(dataRow.OfType<IDisposable>());

                            ITypeInfo[] resolvedTypes = null;
                            var methodToRun = TestMethod;

                            if (methodToRun.IsGenericMethodDefinition)
                            {
                                resolvedTypes = TypeUtility.ResolveGenericTypes(TestCase.TestMethod.Method, dataRow);
                                methodToRun = methodToRun.MakeGenericMethod(resolvedTypes.Select(t => ((IReflectionTypeInfo)t).Type).ToArray());
                            }

                            var parameterTypes = methodToRun.GetParameters().Select(p => p.ParameterType).ToArray();
                            var convertedDataRow = Reflector.ConvertArguments(dataRow, parameterTypes);
                            var theoryDisplayName = TypeUtility.GetDisplayNameWithArguments(TestCase.TestMethod.Method, DisplayName, convertedDataRow, resolvedTypes);
                            var test = new XunitTest(TestCase, theoryDisplayName);

                            testRunners.Add(new BenchmarkTestRunner(test, MessageBus, TestClass, ConstructorArguments, methodToRun, convertedDataRow, SkipReason, BeforeAfterAttributes, Aggregator, CancellationTokenSource));
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Stash the exception so we can surface it during RunTestAsync
                    dataDiscoveryException = ex;
                }
            }

            if (testRunners.Count == 0)
            {
                var test = new XunitTest(TestCase, DisplayName);
                testRunners.Add(new BenchmarkTestRunner(test, MessageBus, TestClass, ConstructorArguments, TestMethod, TestMethodArguments, SkipReason, BeforeAfterAttributes, Aggregator, CancellationTokenSource));
            }
        }

        protected override Task BeforeTestCaseFinishedAsync()
        {
            Aggregator.Aggregate(cleanupAggregator);

            return base.BeforeTestCaseFinishedAsync();
        }

        protected override async Task<RunSummary> RunTestAsync()
        {
            if (dataDiscoveryException != null)
                return RunTest_DataDiscoveryException();

            var runSummary = new RunSummary();

            foreach (var testRunner in testRunners)
                runSummary.Aggregate(await testRunner.RunAsync());

            // Run the cleanup here so we can include cleanup time in the run summary,
            // but save any exceptions so we can surface them during the cleanup phase,
            // so they get properly reported as test case cleanup failures.
            var timer = new ExecutionTimer();
            foreach (var disposable in toDispose)
                timer.Aggregate(() => cleanupAggregator.Run(() => disposable.Dispose()));

            runSummary.Time += timer.Total;

            return runSummary;
        }

        RunSummary RunTest_DataDiscoveryException()
        {
            var test = new XunitTest(TestCase, DisplayName);

            if (!MessageBus.QueueMessage(new TestStarting(test)))
                CancellationTokenSource.Cancel();
            else if (!MessageBus.QueueMessage(new TestFailed(test, 0, null, Unwrap(dataDiscoveryException))))
                CancellationTokenSource.Cancel();
            if (!MessageBus.QueueMessage(new TestFinished(test, 0, null)))
                CancellationTokenSource.Cancel();

            return new RunSummary { Total = 1, Failed = 1 };
        }

        public static Exception Unwrap(Exception ex)
        {
            while (true)
            {
                var tiex = ex as TargetInvocationException;
                if (tiex == null)
                    return ex;

                ex = tiex.InnerException;
            }
        }
    }
}
