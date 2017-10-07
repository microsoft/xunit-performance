using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Api
{
    internal sealed class DefaultPerformanceMetricFactory : IPerformanceMetricFactory
    {
        public IEnumerable<PerformanceMetric> GetMetrics()
        {
            return new List<PerformanceMetric>();
        }
    }
}
