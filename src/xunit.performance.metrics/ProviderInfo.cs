using System;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance
{
    [Serializable]
    public abstract class ProviderInfo
    {
        internal ProviderInfo() { } // Only allow subclassing from this assembly

        public ulong Keywords { get; set; } = unchecked((ulong)-1);

        public static IEnumerable<ProviderInfo> Merge(IEnumerable<ProviderInfo> info)
        {
            Dictionary<Guid, UserProviderInfo> userInfo = new Dictionary<Guid, UserProviderInfo>();
            KernelProviderInfo kernelInfo = new KernelProviderInfo();

            foreach (var i in info)
                i.MergeInto(userInfo, kernelInfo);

            foreach (var i in userInfo.Values)
                yield return i;

            if (kernelInfo.Keywords != 0)
                yield return kernelInfo;
        }

        protected abstract void MergeInto(Dictionary<Guid, UserProviderInfo> userInfo, KernelProviderInfo kernelInfo);
    }
}
