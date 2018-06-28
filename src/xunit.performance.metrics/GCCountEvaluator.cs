// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Xunit.Performance.Sdk;

namespace Microsoft.Xunit.Performance
{
    partial class GCCountMetricDiscoverer : IPerformanceMetricDiscoverer
    {
        class GCCountEvaluator : PerformanceMetricEvaluator
        {
            readonly PerformanceMetricEvaluationContext _context;
            int _count;

            public GCCountEvaluator(PerformanceMetricEvaluationContext context)
            {
                _context = context;
                _context.TraceEventSource.Clr.GCStart += GCStart;
            }

            public override void BeginIteration(TraceEvent beginEvent) => _count = 0;

            public override double EndIteration(TraceEvent endEvent) => _count;

            void GCStart(GCStartTraceData ev)
            {
                if (_context.IsTestEvent(ev))
                    _count++;
            }
        }
    }
}