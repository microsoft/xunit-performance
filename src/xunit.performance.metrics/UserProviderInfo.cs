using Microsoft.Diagnostics.Tracing;
using System;
using Microsoft.Diagnostics.Tracing.Session;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance
{
    [Serializable]
    public sealed class UserProviderInfo : ProviderInfo
    {
        public Guid ProviderGuid { get; set; }
        public TraceEventLevel Level { get; set; } = TraceEventLevel.Verbose;

        protected override void MergeInto(Dictionary<Guid, UserProviderInfo> userInfo, KernelProviderInfo kernelInfo)
        {
            UserProviderInfo current;
            if (!userInfo.TryGetValue(ProviderGuid, out current))
            {
                userInfo.Add(ProviderGuid, this);
            }
            else
            {
                current.Keywords |= Keywords;
                if (Level > current.Level)
                    current.Level = Level;
            }
        }
    }
}
