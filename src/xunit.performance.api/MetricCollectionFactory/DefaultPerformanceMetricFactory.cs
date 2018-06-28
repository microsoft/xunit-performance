using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Api
{
    sealed class DefaultPerformanceMetricFactory : IPerformanceMetricFactory
    {
        public IEnumerable<PerformanceMetric> GetMetrics() => new List<PerformanceMetric>();
    }
}