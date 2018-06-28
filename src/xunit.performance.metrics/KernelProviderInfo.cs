// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance
{
    [Serializable]
    public sealed class KernelProviderInfo : ProviderInfo
    {
        public ulong Keywords { get; set; }

        public ulong StackKeywords { get; set; }

        internal override void MergeInto(Dictionary<Guid, UserProviderInfo> userInfo, KernelProviderInfo kernelInfo, Dictionary<string, CpuCounterInfo> cpuInfo)
        {
            kernelInfo.Keywords |= Keywords;
            kernelInfo.StackKeywords |= StackKeywords;
        }
    }
}