using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance.Api
{
    internal sealed class GenericPerformanceMonitorCounterDiscoverer<T> : IPerformanceMetricDiscoverer
        where T : IPerformanceMonitorCounter, new()
    {
        public GenericPerformanceMonitorCounterDiscoverer()
        {
            _performanceMonitorCounter = new T();
            _pmcId = PerformanceMetric.GetProfileSourceInfoId(_performanceMonitorCounter.Name);
        }

        public IEnumerable<PerformanceMetricInfo> GetMetrics(IAttributeInfo metricAttribute)
        {
            if (_pmcId != -1)
            {
                var interval = (int)(metricAttribute.GetConstructorArguments().FirstOrDefault() ?? _performanceMonitorCounter.Interval);
                yield return new GenericPerformanceMonitorCounterMetric<T>(_performanceMonitorCounter);
            }
        }

        private readonly int _pmcId;
        private readonly T _performanceMonitorCounter;
    }
}
