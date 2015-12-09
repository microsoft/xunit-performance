using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance
{
    internal class ExceptionsMetricDiscoverer : IPerformanceMetricDiscoverer
    {
        public IEnumerable<PerformanceMetricInfo> GetMetrics(IAttributeInfo metricAttribute)
        {
            yield return new ExceptionsMetric();
        }

        private class ExceptionsMetric : PerformanceMetric
        {
            public ExceptionsMetric()
                : base("Exceptions", "Exceptions", PerformanceMetricUnits.Count)
            {
            }

            public override IEnumerable<ProviderInfo> ProviderInfo
            {
                get
                {
                    yield return new UserProviderInfo()
                    {
                        ProviderGuid = ClrTraceEventParser.ProviderGuid,
                        Level = TraceEventLevel.Verbose,
                        Keywords = (ulong)ClrTraceEventParser.Keywords.Exception
                    };
                }
            }

            public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context)
            {
                return new ExceptionsEvaluator(context);
            }
        }

        private class ExceptionsEvaluator : PerformanceMetricEvaluator
        {
            private readonly PerformanceMetricEvaluationContext _context;
            private long _count;

            public ExceptionsEvaluator(PerformanceMetricEvaluationContext context)
            {
                _context = context;
                context.TraceEventSource.Clr.ExceptionStart += Clr_ExceptionStart;
            }

            private void Clr_ExceptionStart(TraceEvent data)
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
