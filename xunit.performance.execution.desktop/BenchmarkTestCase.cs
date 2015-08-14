using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Xunit.Performance
{
    internal class BenchmarkTestCase : XunitTestCase
    {
        public bool SkipWarmup { get; protected set; }
        public bool DiscoverArguments { get; protected set; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public BenchmarkTestCase() { }

        public BenchmarkTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, ITestMethod testMethod, IAttributeInfo attr, object[] testMethodArguments = null)
            : base(diagnosticMessageSink, defaultMethodDisplay, testMethod, testMethodArguments)
        {
            DiscoverArguments = testMethodArguments == null;
            SkipWarmup = attr.GetNamedArgument<bool>(nameof(BenchmarkAttribute.SkipWarmup));
        }

        /// <inheritdoc />
        public override Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink,
                                                  IMessageBus messageBus,
                                                  object[] constructorArguments,
                                                  ExceptionAggregator aggregator,
                                                  CancellationTokenSource cancellationTokenSource)
        {
            return new BenchmarkTestCaseRunner(this, DisplayName, SkipReason, constructorArguments, DiscoverArguments ? null : TestMethodArguments, diagnosticMessageSink, messageBus, aggregator, cancellationTokenSource).RunAsync();
        }

        public override void Serialize(IXunitSerializationInfo data)
        {
            base.Serialize(data);
            data.AddValue(nameof(SkipWarmup), SkipWarmup);
            data.AddValue(nameof(DiscoverArguments), DiscoverArguments);
        }

        public override void Deserialize(IXunitSerializationInfo data)
        {
            SkipWarmup = data.GetValue<bool>(nameof(SkipWarmup));
            DiscoverArguments = data.GetValue<bool>(nameof(DiscoverArguments));
            base.Deserialize(data);
        }
    }
}
