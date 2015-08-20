using Microsoft.Diagnostics.Tracing.Parsers;
using System;
using Microsoft.Diagnostics.Tracing.Session;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance
{
    [Serializable]
    public sealed class KernelProviderInfo : ProviderInfo
    {
        public ulong StackKeywords { get; set; }

        protected override void MergeInto(Dictionary<Guid, UserProviderInfo> userInfo, KernelProviderInfo kernelInfo)
        {
            kernelInfo.Keywords |= Keywords;
            kernelInfo.StackKeywords |= StackKeywords;
        }
    }
}
