using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance
{
    internal class ModuleLoadMetricDiscoverer : IPerformanceMetricDiscoverer
    {
        public IEnumerable<PerformanceMetricInfo> GetMetrics(IAttributeInfo metricAttribute)
        {
            yield return new ModuleLoadMetric();
        }

        private class ModuleLoadMetric : PerformanceMetric
        {
            public ModuleLoadMetric()
                : base("ModuleLoad", "Modules Loaded", PerformanceMetricUnits.Count)
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
                        Keywords = (ulong)ClrTraceEventParser.Keywords.Loader
                    };
                }
            }

            public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context)
            {
                return new ModuleLoadEvaluator(context);
            }
        }

        private class ModuleLoadEvaluator : PerformanceMetricEvaluator
        {
            private readonly PerformanceMetricEvaluationContext _context;
            private long _count;

            public ModuleLoadEvaluator(PerformanceMetricEvaluationContext context)
            {
                _context = context;
                context.TraceEventSource.Clr.LoaderModuleLoad += Clr_ModuleLoad;
            }

            private void Clr_ModuleLoad(TraceEvent data)
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
