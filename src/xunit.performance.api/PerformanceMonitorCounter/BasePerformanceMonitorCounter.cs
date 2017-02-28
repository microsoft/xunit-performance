using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Api
{
    internal abstract class BasePerformanceMonitorCounter : PerformanceMetric
    {
        public BasePerformanceMonitorCounter(IPerformanceMonitorCounter pmc) : base(pmc.Name, pmc.DisplayName, pmc.Unit)
        {
            _interval = pmc.Interval;
            if (TraceEventProfileSources.GetInfo().TryGetValue(Id, out ProfileSourceInfo profileSourceInfo))
                _profileSourceInfoID = profileSourceInfo.ID;
            else
                _profileSourceInfoID = -1;
        }

        public override IEnumerable<ProviderInfo> ProviderInfo
        {
            get
            {
                yield return new KernelProviderInfo()
                {
                    Keywords = unchecked((ulong)(KernelTraceEventParser.Keywords.PMCProfile | KernelTraceEventParser.Keywords.Profile)),
                };
                yield return new CpuCounterInfo()
                {
                    CounterName = Id,
                    Interval = _interval,
                };
            }
        }

        public bool IsValidPmc => _profileSourceInfoID > -1;

        protected int ProfileSourceInfoID => _profileSourceInfoID;

        private readonly int _interval;
        private readonly int _profileSourceInfoID;
    }
}
