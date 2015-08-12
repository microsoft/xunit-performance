using Microsoft.Diagnostics.Tracing;
using System;
using Microsoft.Diagnostics.Tracing.Session;

namespace Microsoft.Xunit.Performance
{
    [Serializable]
    public class UserProviderInfo : ProviderInfo
    {
        public Guid ProviderGuid { get; set; }
        public TraceEventLevel Level { get; set; } = TraceEventLevel.Verbose;

        internal override void Enable(TraceEventSession session)
        {
            session.EnableProvider(ProviderGuid, Level, Keywords);
        }
    }
}
