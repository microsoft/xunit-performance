using Microsoft.Diagnostics.Tracing.Parsers;
using System;
using Microsoft.Diagnostics.Tracing.Session;

namespace Microsoft.Xunit.Performance
{
    [Serializable]
    public sealed class KernelProviderInfo : ProviderInfo
    {
        public ulong StackKeywords { get; set; }
    }
}
