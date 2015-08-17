using Microsoft.Diagnostics.Tracing.Session;
using System;

namespace Microsoft.Xunit.Performance
{
    [Serializable]
    public abstract class ProviderInfo
    {
        public ulong Keywords { get; set; } = unchecked((ulong)-1);

        internal abstract void Enable(TraceEventSession session);
    }
}
