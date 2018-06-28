using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Api
{
    class GCAllocatedBytesForCurrentThreadMetric : PerformanceMetric
    {
        public GCAllocatedBytesForCurrentThreadMetric()
            : base(GetAllocatedBytesForCurrentThreadId, GetAllocatedBytesForCurrentThreadDisplayName, PerformanceMetricUnits.Bytes)
        {
        }

        public static string GetAllocatedBytesForCurrentThreadDisplayName => "Allocation Size on Benchmark Execution Thread";
        public static string GetAllocatedBytesForCurrentThreadId => "GC.GetAllocatedBytesForCurrentThread";

        public override IEnumerable<ProviderInfo> ProviderInfo
        {
            get
            {
                yield return new UserProviderInfo()
                {
                    ProviderGuid = MicrosoftXunitBenchmarkTraceEventParser.ProviderGuid,
                    Level = TraceEventLevel.Always,
                    Keywords = (ulong)KernelTraceEventParser.Keywords.None
                };
            }
        }

        public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context) => new GCAllocatedBytesForCurrentThreadEvaluator();
    }
}