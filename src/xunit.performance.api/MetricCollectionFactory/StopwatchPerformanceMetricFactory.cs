using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;
using static Microsoft.Xunit.Performance.BenchmarkMetricDiscoverer;

namespace Microsoft.Xunit.Performance.Api
{
    sealed class StopwatchPerformanceMetricFactory : IPerformanceMetricFactory
    {
        public IEnumerable<PerformanceMetric> GetMetrics() => new List<PerformanceMetric> { new BenchmarkDurationMetric() };
    }
}