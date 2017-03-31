using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance.Api
{
    internal sealed class GCAllocatedBytesForCurrentThreadMetricDiscoverer : IPerformanceMetricDiscoverer
    {
        public IEnumerable<PerformanceMetricInfo> GetMetrics(IAttributeInfo metricAttribute)
        {
            yield return new GCAllocatedBytesForCurrentThreadMetric();
        }
    }
}
