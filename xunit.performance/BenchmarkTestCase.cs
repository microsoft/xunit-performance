using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Xunit.Performance
{
    internal class BenchmarkTestCase : XunitTestCase
    {
        internal double MarginOfError { get; private set; }
        internal double Confidence { get; private set; }
        internal bool? TriggersGC { get; private set; }
        internal bool SkipWarmup { get; private set; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public BenchmarkTestCase() { }

        public BenchmarkTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, ITestMethod testMethod, IAttributeInfo attr)
            : base(diagnosticMessageSink, defaultMethodDisplay, testMethod)
        {
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

            TriggersGC = attr.GetNamedArgument<bool?>(nameof(BenchmarkAttribute.TriggersGC));
            SkipWarmup = attr.GetNamedArgument<bool>(nameof(BenchmarkAttribute.SkipWarmup));
        }

        /// <inheritdoc />
        public override Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink,
                                                  IMessageBus messageBus,
                                                  object[] constructorArguments,
                                                  ExceptionAggregator aggregator,
                                                  CancellationTokenSource cancellationTokenSource)
        {
            return new BenchmarkTestCaseRunner(this, DisplayName, SkipReason, constructorArguments, diagnosticMessageSink, messageBus, aggregator, cancellationTokenSource).RunAsync();
        }

        public override void Serialize(IXunitSerializationInfo data)
        {
            base.Serialize(data);
            data.AddValue(nameof(MarginOfError), MarginOfError);
            data.AddValue(nameof(Confidence), Confidence);
            data.AddValue(nameof(TriggersGC), TriggersGC);
            data.AddValue(nameof(SkipWarmup), SkipWarmup);
        }

        public override void Deserialize(IXunitSerializationInfo data)
        {
            MarginOfError = data.GetValue<double>(nameof(MarginOfError));
            Confidence = data.GetValue<double>(nameof(Confidence));
            TriggersGC = data.GetValue<bool?>(nameof(TriggersGC));
            SkipWarmup = data.GetValue<bool>(nameof(SkipWarmup));
            base.Deserialize(data);
        }
    }
}
