using System;
using System.Collections.Generic;
using Microsoft.Xunit.Performance.Sdk;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance
{
    class BenchmarkMetricDiscoverer : IPerformanceMetricDiscoverer
    {
        static readonly PerformanceMetric[] _metrics = new[] { new BenchmarkDurationMetric() };

        public IEnumerable<PerformanceMetric> GetMetrics(IAttributeInfo metricAttribute) => _metrics;
    }
}
