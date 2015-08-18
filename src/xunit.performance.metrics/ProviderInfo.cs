using Microsoft.Diagnostics.Tracing.Session;
using System;

namespace Microsoft.Xunit.Performance
{
    public abstract class ProviderInfo
    {
        internal ProviderInfo() { } // Only allow subclassing from this assembly

        public ulong Keywords { get; set; } = unchecked((ulong)-1);
    }
}
