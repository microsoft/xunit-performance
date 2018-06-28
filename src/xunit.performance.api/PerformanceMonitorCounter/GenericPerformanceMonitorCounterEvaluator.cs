using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Xunit.Performance.Sdk;
using System.Diagnostics;

namespace Microsoft.Xunit.Performance.Api
{
    class GenericPerformanceMonitorCounterEvaluator<T> : PerformanceMetricEvaluator
        where T : IPerformanceMonitorCounter, new()
    {
        PerformanceMetricEvaluationContext _context;
        long _count;
        int _interval;
        int _profileSource;

        public override void BeginIteration(TraceEvent beginEvent) => _count = 0;

        public override double EndIteration(TraceEvent endEvent) => _count;

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
                    // FIXME: We should fail here instead.
                    Debug.Assert(_context == context);
                    Debug.Assert(_profileSource == profileSourceInfoID);
                }
            }
        }

        void PerfInfoCollectionStart(SampledProfileIntervalTraceData ev)
        {
            if (ev.SampleSource == _profileSource)
                _interval = ev.NewInterval;
        }

        void PerfInfoPMCSample(PMCCounterProfTraceData ev)
        {
            if (ev.ProfileSource == _profileSource && _context.IsTestEvent(ev))
                _count += _interval;
        }
    }
}