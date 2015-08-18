using Microsoft.Diagnostics.Tracing;
using System;
using Microsoft.Diagnostics.Tracing.Session;

namespace Microsoft.Xunit.Performance
{
    [Serializable]
    public sealed class UserProviderInfo : ProviderInfo
    {
        public Guid ProviderGuid { get; set; }
        public TraceEventLevel Level { get; set; } = TraceEventLevel.Verbose;
    }
}
