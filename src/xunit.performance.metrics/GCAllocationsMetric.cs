// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance
{
    partial class GCAllocationsMetricDiscoverer : IPerformanceMetricDiscoverer
    {
        internal class GCAllocationsMetric : PerformanceMetric
        {
            public GCAllocationsMetric()
                : base("GCAlloc", "GC Allocations", PerformanceMetricUnits.Bytes)
            {
            }

            public override IEnumerable<ProviderInfo> ProviderInfo
            {
                get
                {
                    yield return new UserProviderInfo()
                    {
                        ProviderGuid = ClrTraceEventParser.ProviderGuid,
                        Level = TraceEventLevel.Verbose,
                        Keywords = (ulong)ClrTraceEventParser.Keywords.GC
                    };
                }
            }

            public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context) => new GCAllocationsEvaluator(context);
        }
    }
}