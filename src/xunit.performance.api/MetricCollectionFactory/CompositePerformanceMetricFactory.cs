using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Api
{
    sealed class CompositePerformanceMetricFactory : IPerformanceMetricFactory
    {
        readonly List<IPerformanceMetricFactory> _factories;

        public CompositePerformanceMetricFactory() => _factories = new List<IPerformanceMetricFactory>();

        public void Add(IPerformanceMetricFactory metricCollectionFactory) => _factories.Add(metricCollectionFactory);

        public IEnumerable<PerformanceMetric> GetMetrics()
        {
            var metrics = new List<PerformanceMetric>();
            foreach (var factory in _factories)
                metrics.AddRange(factory.GetMetrics());
            return metrics;
        }
    }
}