// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance
{
    [Serializable]
    public abstract class ProviderInfo
    {
        internal ProviderInfo()
        {
        } // Only allow subclassing from this assembly

        public static IEnumerable<ProviderInfo> Merge(IEnumerable<ProviderInfo> info)
        {
            var userInfo = new Dictionary<Guid, UserProviderInfo>();
            var kernelInfo = new KernelProviderInfo();
            var cpuInfo = new Dictionary<string, CpuCounterInfo>();

            foreach (var i in info)
                i.MergeInto(userInfo, kernelInfo, cpuInfo);

            if (kernelInfo.Keywords != 0)
                yield return kernelInfo;

            foreach (var i in userInfo.Values)
                yield return i;

            foreach (var i in cpuInfo.Values)
                yield return i;
        }

        internal abstract void MergeInto(Dictionary<Guid, UserProviderInfo> userInfo, KernelProviderInfo kernelInfo, Dictionary<string, CpuCounterInfo> cpuInfo);
    }
}