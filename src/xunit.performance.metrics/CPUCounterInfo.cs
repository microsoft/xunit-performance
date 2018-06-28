// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance
{
    [Serializable]
    public sealed class CpuCounterInfo : ProviderInfo
    {
        public string CounterName { get; set; }
        public int Interval { get; set; }

        internal override void MergeInto(Dictionary<Guid, UserProviderInfo> userInfo, KernelProviderInfo kernelInfo, Dictionary<string, CpuCounterInfo> cpuInfo)
        {
            if (!cpuInfo.TryGetValue(CounterName, out var current))
            {
                cpuInfo.Add(CounterName, this);
            }
            else
            {
                cpuInfo[CounterName] = new CpuCounterInfo()
                {
                    CounterName = CounterName,
                    Interval = Math.Min(current.Interval, Interval)
                };
            }
        }
    }
}