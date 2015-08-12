using Microsoft.Diagnostics.Tracing.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
