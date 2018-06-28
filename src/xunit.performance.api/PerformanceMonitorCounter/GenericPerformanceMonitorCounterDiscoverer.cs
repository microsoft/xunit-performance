using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance.Api
{
    sealed class GenericPerformanceMonitorCounterDiscoverer<T> : IPerformanceMetricDiscoverer
        where T : IPerformanceMonitorCounter, new()
    {
        readonly T _performanceMonitorCounter;

        readonly int _pmcId;

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
    }
}