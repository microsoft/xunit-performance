using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Xunit.Performance
{
    internal class BenchmarkTestCase : XunitTestCase
    {
        public virtual double MarginOfError { get; protected set; }
        public virtual double Confidence { get; protected set; }
        public bool SkipWarmup { get; protected set; }
        public bool DiscoverArguments { get; protected set; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public BenchmarkTestCase() { }

        public BenchmarkTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, ITestMethod testMethod, IAttributeInfo attr, object[] testMethodArguments = null)
            : base(diagnosticMessageSink, defaultMethodDisplay, testMethod, testMethodArguments)
        {
            DiscoverArguments = testMethodArguments == null;

            MarginOfError = attr.GetNamedArgument<double>(nameof(BenchmarkAttribute.MarginOfError));
            if (MarginOfError == default(double))
                MarginOfError = BenchmarkAttribute.DefaultMarginOfError;
            if (MarginOfError <= 0.0 || MarginOfError >= 1.0)
                throw new InvalidOperationException($"Invalid {nameof(MarginOfError)}.  Must be greater than 0.0, and less than 1.0.");

            Confidence = attr.GetNamedArgument<double>(nameof(BenchmarkAttribute.Confidence));
            if (Confidence == default(double))
                Confidence = BenchmarkAttribute.DefaultConfidence;
            if (Confidence <= 0.0 || Confidence >= 1.0)
                throw new InvalidOperationException($"Invalid {nameof(Confidence)}.  Must be greater than 0.0, and less than 1.0.");

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
            data.AddValue(nameof(MarginOfError), MarginOfError);
            data.AddValue(nameof(Confidence), Confidence);
            data.AddValue(nameof(SkipWarmup), SkipWarmup);
            data.AddValue(nameof(DiscoverArguments), DiscoverArguments);
        }

        public override void Deserialize(IXunitSerializationInfo data)
        {
            MarginOfError = data.GetValue<double>(nameof(MarginOfError));
            Confidence = data.GetValue<double>(nameof(Confidence));
            SkipWarmup = data.GetValue<bool>(nameof(SkipWarmup));
            DiscoverArguments = data.GetValue<bool>(nameof(DiscoverArguments));
            base.Deserialize(data);
        }
    }
}
