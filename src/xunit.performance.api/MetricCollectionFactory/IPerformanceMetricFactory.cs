using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Api
{
    interface IPerformanceMetricFactory
    {
        /// <summary>
        /// Gets the list of performance metrics that needs to be collected.
        /// </summary>
        /// <param name="assemblyFileName"></param>
        /// <returns></returns>
        IEnumerable<PerformanceMetric> GetMetrics(string assemblyFileName);
    }
}
