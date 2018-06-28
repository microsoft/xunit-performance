using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Api
{
    abstract class BasePerformanceMonitorCounter : PerformanceMetric
    {
        readonly int _interval;

        protected BasePerformanceMonitorCounter(IPerformanceMonitorCounter pmc) : base(pmc.Name, pmc.DisplayName, pmc.Unit)
        {
            _interval = pmc.Interval;
            ProfileSourceInfoID = GetProfileSourceInfoId(Id);
        }

        public bool IsValidPmc => ProfileSourceInfoID > -1;

        public override IEnumerable<ProviderInfo> ProviderInfo
        {
            get
            {
                yield return new KernelProviderInfo()
                {
                    Keywords = unchecked((ulong)KernelTraceEventParser.Keywords.PMCProfile),
                    StackKeywords = unchecked((ulong)KernelTraceEventParser.Keywords.PMCProfile),
                };
                yield return new CpuCounterInfo()
                {
                    CounterName = Id,
                    Interval = _interval,
                };
            }
        }

        protected int ProfileSourceInfoID { get; private set; }
    }
}