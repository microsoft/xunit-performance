// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Xunit.Performance
{
    class BenchmarkTestCase : XunitTestCase
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public BenchmarkTestCase() { }

        public BenchmarkTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, ITestMethod testMethod, IAttributeInfo attr, object[] testMethodArguments = null)
            : base(diagnosticMessageSink, defaultMethodDisplay, testMethod, testMethodArguments) => DiscoverArguments = testMethodArguments == null;

        public bool DiscoverArguments { get; protected set; }

        public override void Deserialize(IXunitSerializationInfo data)
        {
            DiscoverArguments = data.GetValue<bool>(nameof(DiscoverArguments));
            base.Deserialize(data);
        }

        /// <inheritdoc />
        public override Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink,
                                                  IMessageBus messageBus,
                                                  object[] constructorArguments,
                                                  ExceptionAggregator aggregator,
                                                  CancellationTokenSource cancellationTokenSource) => new BenchmarkTestCaseRunner(this, DisplayName, SkipReason, constructorArguments, DiscoverArguments ? null : TestMethodArguments, diagnosticMessageSink, messageBus, aggregator, cancellationTokenSource).RunAsync();

        public override void Serialize(IXunitSerializationInfo data)
        {
            base.Serialize(data);
            data.AddValue(nameof(DiscoverArguments), DiscoverArguments);
        }
    }
}