using Microsoft.Diagnostics.Tracing.Parsers;
using System;
using Microsoft.Diagnostics.Tracing.Session;

namespace Microsoft.Xunit.Performance
{
    [Serializable]
    public class KernelProviderInfo : ProviderInfo
    {
        public ulong StackKeywords { get; set; }

        internal override void Enable(TraceEventSession session)
        {
            session.EnableKernelProvider((KernelTraceEventParser.Keywords)Keywords, (KernelTraceEventParser.Keywords)StackKeywords);
        }
    }
}
