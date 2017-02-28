using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Xunit.Performance.Sdk;
using System.Diagnostics;

namespace Microsoft.Xunit.Performance.Api
{
    internal class GenericPerformanceMonitorCounterEvaluator<T> : PerformanceMetricEvaluator
        where T : IPerformanceMonitorCounter, new()
    {
        private PerformanceMetricEvaluationContext _context;
        private int _profileSource;
        private int _interval;
        private long _count;

        internal void Initialize(PerformanceMetricEvaluationContext context, int profileSourceInfoID)
        {
            lock (this)
            {
                if (_context == null)
                {
                    context.TraceEventSource.Kernel.PerfInfoCollectionStart += PerfInfoCollectionStart;
                    context.TraceEventSource.Kernel.PerfInfoPMCSample += PerfInfoPMCSample;
                    _context = context;
                    _profileSource = profileSourceInfoID;
                }
                else
                {
                    Debug.Assert(_context == context);
                    Debug.Assert(_profileSource == profileSourceInfoID);
                }
            }
        }

        private void PerfInfoCollectionStart(SampledProfileIntervalTraceData ev)
        {
            if (ev.SampleSource == _profileSource)
                _interval = ev.NewInterval;
        }

        private void PerfInfoPMCSample(PMCCounterProfTraceData ev)
        {
            if (ev.ProfileSource == _profileSource && _context.IsTestEvent(ev))
                _count += _interval;
        }

        public override void BeginIteration(TraceEvent beginEvent)
        {
            _count = 0;
        }

        public override double EndIteration(TraceEvent endEvent)
        {
            return _count;
        }
    }
}
