using Microsoft.Xunit.Performance.Sdk;
using System;
using System.Collections.Generic;
using static Microsoft.Xunit.Performance.Api.Common;
using static Microsoft.Xunit.Performance.Api.PerformanceLogger;
using static Microsoft.Xunit.Performance.InstructionsRetiredMetricDiscoverer;

namespace Microsoft.Xunit.Performance.Api
{
    sealed class PmcPerformanceMetricFactory : IPerformanceMetricFactory
    {
        readonly List<PerformanceMetric> _metrics;

        public PmcPerformanceMetricFactory(string pmcName)
        {
            _metrics = new List<PerformanceMetric>();

            if (IsWindowsPlatform)
            {
                if (pmcName.Equals("BranchMispredictions", StringComparison.OrdinalIgnoreCase))
                {
                    _metrics.Add(new GenericPerformanceMonitorCounterMetric<BranchMispredictionsPerformanceMonitorCounter>(
                        new BranchMispredictionsPerformanceMonitorCounter()));
                }
                else if (pmcName.Equals("CacheMisses", StringComparison.OrdinalIgnoreCase))
                {
                    _metrics.Add(new GenericPerformanceMonitorCounterMetric<CacheMissesPerformanceMonitorCounter>(
                        new CacheMissesPerformanceMonitorCounter()));
                }
                else if (pmcName.Equals("InstructionRetired", StringComparison.OrdinalIgnoreCase))
                {
                    _metrics.Add(new InstructionsRetiredMetric());
                }
                else
                {
                    throw new NotSupportedException($"Unsupported performance monitor counter: {pmcName}");
                }
            }
            else
            {
                WriteWarningLine("Performance Monitor Counters are not supported in this platform.");
            }
        }

        public IEnumerable<PerformanceMetric> GetMetrics() => _metrics;
    }
}