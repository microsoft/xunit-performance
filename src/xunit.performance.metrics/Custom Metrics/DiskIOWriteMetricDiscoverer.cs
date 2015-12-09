using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance
{
    internal class DiskIOWriteMetricDiscoverer : IPerformanceMetricDiscoverer
    {
        public IEnumerable<PerformanceMetricInfo> GetMetrics(IAttributeInfo metricAttribute)
        {
            yield return new DiskIOWriteMetric();
        }

        private class DiskIOWriteMetric : PerformanceMetric
        {
            public DiskIOWriteMetric()
                : base("DiskIOWrite", "Disk IO Writes", PerformanceMetricUnits.Count)
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
                return new DiskIOWriteEvaluator(context);
            }
        }

        private class DiskIOWriteEvaluator : PerformanceMetricEvaluator
        {
            private readonly PerformanceMetricEvaluationContext _context;
            private long _count;

            public DiskIOWriteEvaluator(PerformanceMetricEvaluationContext context)
            {
                _context = context;
                context.TraceEventSource.Kernel.DiskIOWrite += Kernel_DiskIOWrite;
            }

            private void Kernel_DiskIOWrite(TraceEvent data)
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
