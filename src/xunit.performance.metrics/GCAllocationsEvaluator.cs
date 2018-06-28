// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Xunit.Performance.Sdk;

namespace Microsoft.Xunit.Performance
{
    partial class GCAllocationsMetricDiscoverer : IPerformanceMetricDiscoverer
    {
        class GCAllocationsEvaluator : PerformanceMetricEvaluator
        {
            readonly PerformanceMetricEvaluationContext _context;
            long _bytes;

            public GCAllocationsEvaluator(PerformanceMetricEvaluationContext context)
            {
                _context = context;
                context.TraceEventSource.Clr.GCAllocationTick += Clr_GCAllocationTick;
            }

            public override void BeginIteration(TraceEvent beginEvent) => _bytes = 0;

            public override double EndIteration(TraceEvent endEvent) => _bytes;

            void Clr_GCAllocationTick(GCAllocationTickTraceData ev)
            {
                if (_context.IsTestEvent(ev))
                    _bytes += ev.AllocationAmount64;
            }
        }
    }
}