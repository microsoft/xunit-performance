using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;
using static Microsoft.Xunit.Performance.BenchmarkMetricDiscoverer;

namespace Microsoft.Xunit.Performance.Api
{
    internal sealed class StopwatchPerformanceMetricFactory : IPerformanceMetricFactory
    {
        public IEnumerable<PerformanceMetric> GetMetrics(string assemblyFileName)
        {
            return new List<PerformanceMetric> { new BenchmarkDurationMetric() };
        }
    }
}
