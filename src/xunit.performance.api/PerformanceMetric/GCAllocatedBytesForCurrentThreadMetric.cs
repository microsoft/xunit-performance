using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Api
{
    internal class GCAllocatedBytesForCurrentThreadMetric : PerformanceMetric
    {
        public GCAllocatedBytesForCurrentThreadMetric()
            : base("GCAllocatedBytesForCurrentThread", "Allocations In Current Thread", PerformanceMetricUnits.Bytes)
        {
        }

        public override IEnumerable<ProviderInfo> ProviderInfo
        {
            get
            {
                yield return new UserProviderInfo()
                {
                    ProviderGuid = MicrosoftXunitBenchmarkTraceEventParser.ProviderGuid,
                    Level = TraceEventLevel.Informational,
                    Keywords = ulong.MaxValue
                };
            }
        }

        public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context)
        {
            return new GCAllocatedBytesForCurrentThreadEvaluator(context);
        }
    }
}
