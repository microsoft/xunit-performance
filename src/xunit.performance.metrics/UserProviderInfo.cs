// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance
{
    [Serializable]
    public sealed class UserProviderInfo : ProviderInfo
    {
        public ulong Keywords { get; set; }
        public TraceEventLevel Level { get; set; } = TraceEventLevel.Verbose;
        public Guid ProviderGuid { get; set; }

        internal override void MergeInto(Dictionary<Guid, UserProviderInfo> userInfo, KernelProviderInfo kernelInfo, Dictionary<string, CpuCounterInfo> cpuInfo)
        {
            if (!userInfo.TryGetValue(ProviderGuid, out var current))
            {
                userInfo.Add(ProviderGuid, this);
            }
            else
            {
                userInfo[ProviderGuid] = new UserProviderInfo()
                {
                    ProviderGuid = ProviderGuid,
                    Keywords = current.Keywords | Keywords,
                    Level = (Level > current.Level) ? Level : current.Level
                };
            }
        }
    }
}