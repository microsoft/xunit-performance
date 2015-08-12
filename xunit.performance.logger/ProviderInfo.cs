using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Xunit.Performance
{
    [Serializable]
    public abstract class ProviderInfo
    {
        public ulong Keywords { get; set; } = unchecked((ulong)-1);

        internal abstract void Enable(TraceEventSession session);
    }
}
