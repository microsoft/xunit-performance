using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;
using System;

namespace Microsoft.Xunit.Performance.Api
{
    internal sealed class CompositePerformanceMetricFactory : IPerformanceMetricFactory
    {
        public CompositePerformanceMetricFactory()
        {
            _factories = new List<IPerformanceMetricFactory>();
        }

        public IEnumerable<PerformanceMetric> GetMetrics()
        {
            var metrics = new List<PerformanceMetric>();
            foreach (var factory in _factories)
                metrics.AddRange(factory.GetMetrics());
            return metrics;
        }

        public void Add(IPerformanceMetricFactory metricCollectionFactory)
        {
            _factories.Add(metricCollectionFactory);
        }

        private readonly List<IPerformanceMetricFactory> _factories;
    }
}
