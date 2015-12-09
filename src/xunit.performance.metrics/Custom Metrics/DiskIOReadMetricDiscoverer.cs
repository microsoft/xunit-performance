using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance
{
    internal class DiskIOReadMetricDiscoverer : IPerformanceMetricDiscoverer
    {
        public IEnumerable<PerformanceMetricInfo> GetMetrics(IAttributeInfo metricAttribute)
        {
            yield return new DiskIOReadMetric();
        }

        private class DiskIOReadMetric : PerformanceMetric
        {
            public DiskIOReadMetric()
                : base("DiskIORead", "Disk IO Reads", PerformanceMetricUnits.Count)
            {
            }

            public override IEnumerable<ProviderInfo> ProviderInfo
            {
                get
                {
                    yield return new UserProviderInfo()
                    {
                        ProviderGuid = KernelTraceEventParser.ProviderGuid,
                        Level = TraceEventLevel.Verbose,
                        Keywords = (ulong)KernelTraceEventParser.Keywords.DiskIO
                    };
                }
            }

            public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context)
            {
                return new DiskIOReadEvaluator(context);
            }
        }

        private class DiskIOReadEvaluator : PerformanceMetricEvaluator
        {
            private readonly PerformanceMetricEvaluationContext _context;
            private long _count;

            public DiskIOReadEvaluator(PerformanceMetricEvaluationContext context)
            {
                _context = context;
                context.TraceEventSource.Kernel.DiskIORead += Kernel_DiskIORead;
            }

            private void Kernel_DiskIORead(TraceEvent data)
            {
                if (_context.IsTestEvent(data))
                    _count += 1;
            }

            public override void BeginIteration(TraceEvent beginEvent)
            {
                _count = 0;
            }

            public override object EndIteration(TraceEvent endEvent)
            {
                return _count;
            }
        }
    }
}
