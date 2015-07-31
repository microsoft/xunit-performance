using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Xunit.Performance
{
    internal class BenchmarkTestCaseRunner : XunitTestCaseRunner
    {
        static readonly object[] NoArguments = new object[0];

        readonly IMessageSink diagnosticMessageSink;

        public BenchmarkTestCaseRunner(IXunitTestCase testCase,
                                         string displayName,
                                         string skipReason,
                                         object[] constructorArguments,
                                         IMessageSink diagnosticMessageSink,
                                         IMessageBus messageBus,
                                         ExceptionAggregator aggregator,
                                         CancellationTokenSource cancellationTokenSource)
            : base(testCase, displayName, skipReason, constructorArguments, NoArguments, messageBus, aggregator, cancellationTokenSource)
        {
            this.diagnosticMessageSink = diagnosticMessageSink;
        }

        protected override async Task<RunSummary> RunTestAsync()
        {
            var test = new XunitTest(TestCase, DisplayName);

            var runSummary = new RunSummary();
            runSummary.Aggregate(await new BenchmarkTestRunner(test, MessageBus, TestClass, ConstructorArguments, TestMethod, null, SkipReason, BeforeAfterAttributes, Aggregator, CancellationTokenSource).RunAsync());

            return runSummary;
        }
    }
}
