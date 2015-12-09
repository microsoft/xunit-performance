using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance
{
    internal class FilesReadMetricDiscoverer : IPerformanceMetricDiscoverer
    {
        public IEnumerable<PerformanceMetricInfo> GetMetrics(IAttributeInfo metricAttribute)
        {
            yield return new FilesReadMetric();
        }

        private class FilesReadMetric : PerformanceMetric
        {
            public FilesReadMetric()
                : base("FilesRead", "Files Read", PerformanceMetricUnits.List)
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
                        Keywords = (ulong)KernelTraceEventParser.Keywords.FileIO
                    };
                }
            }

            public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context)
            {
                return new FilesReadEvaluator(context);
            }
        }

        private class FilesReadEvaluator : PerformanceMetricEvaluator
        {
            private readonly PerformanceMetricEvaluationContext _context;
            private ListMetricInfo _files;

            public FilesReadEvaluator(PerformanceMetricEvaluationContext context)
            {
                _context = context;
                context.TraceEventSource.Kernel.FileIORead += Kernel_FileIORead;
            }

            private void Kernel_FileIORead(FileIOReadWriteTraceData data)
            {
                if (_context.IsTestEvent(data))
                    _files.addItem(data.FileName, data.IoSize);
            }

            public override void BeginIteration(TraceEvent beginEvent)
            {
                _files = new ListMetricInfo();
            }

            public override object EndIteration(TraceEvent endEvent)
            {
                return _files;
            }
        }
    }
}
