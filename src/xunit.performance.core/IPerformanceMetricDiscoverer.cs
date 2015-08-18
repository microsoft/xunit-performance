using System.Collections.Generic;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance.Sdk
{
    /// <summary>
    /// Implemented by discoverers that provide metrics to performance tests.
    /// </summary> 
    public interface IPerformanceMetricDiscoverer
    {
        /// <summary>
        /// Gets the performance metrics from the metric attribute.
        /// </summary>
        /// <param name="metricAttribute"></param>
        /// <returns></returns>
        IEnumerable<PerformanceMetric> GetMetrics(IAttributeInfo metricAttribute);
    }
}
