using Microsoft.Xunit.Performance.Execution;
using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;
using static Microsoft.Xunit.Performance.Api.PerformanceLogger;

namespace Microsoft.Xunit.Performance.Api
{
    sealed class GCAPIPerformanceMetricFactory : IPerformanceMetricFactory
    {
        readonly List<PerformanceMetric> _metrics;

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

        public IEnumerable<PerformanceMetric> GetMetrics() => _metrics;
    }
}