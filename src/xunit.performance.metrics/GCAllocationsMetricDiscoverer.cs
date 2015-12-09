// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance
{
    internal class GCAllocationsMetricDiscoverer : IPerformanceMetricDiscoverer
    {
        public IEnumerable<PerformanceMetricInfo> GetMetrics(IAttributeInfo metricAttribute)
        {
            yield return new GCAllocationsMetric();
        }

        private class GCAllocationsMetric : PerformanceMetric
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

            public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context)
            {
                return new GCAllocationsEvaluator(context);
            }
        }

        private class GCAllocationsEvaluator : PerformanceMetricEvaluator
        {
            private readonly PerformanceMetricEvaluationContext _context;
            private long _bytes;

            public GCAllocationsEvaluator(PerformanceMetricEvaluationContext context)
            {
                _context = context;
                context.TraceEventSource.Clr.GCAllocationTick += Clr_GCAllocationTick;
            }

            private void Clr_GCAllocationTick(GCAllocationTickTraceData ev)
            {
                if (_context.IsTestEvent(ev))
                    _bytes += ev.AllocationAmount64;
            }

            public override void BeginIteration(TraceEvent beginEvent)
            {
                _bytes = 0;
            }

            public override object EndIteration(TraceEvent endEvent)
            {
                return _bytes;
            }
        }
    }
}
