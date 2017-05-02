using Microsoft.Xunit.Performance.Execution;
using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;
using static Microsoft.Xunit.Performance.Api.PerformanceLogger;

namespace Microsoft.Xunit.Performance.Api
{
    internal sealed class GCAPIPerformanceMetricFactory : IPerformanceMetricFactory
    {
        public GCAPIPerformanceMetricFactory()
        {
            _metrics = new List<PerformanceMetric>();

            if (AllocatedBytesForCurrentThread.IsAvailable)
            {
                _metrics.Add(new GCAllocatedBytesForCurrentThreadMetric());
            }
            else
            {
                WriteWarningLine($"{AllocatedBytesForCurrentThread.NoAvailabilityReason}");
                WriteWarningLine($"The '{GCAllocatedBytesForCurrentThreadMetric.GetAllocatedBytesForCurrentThreadDisplayName}' metric will not be collected.");
            }
        }

        public IEnumerable<PerformanceMetric> GetMetrics(string assemblyFileName)
        {
            return _metrics;
        }

        private readonly List<PerformanceMetric> _metrics;
    }
}
