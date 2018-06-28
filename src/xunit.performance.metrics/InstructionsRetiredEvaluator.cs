// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Xunit.Performance.Sdk;
using System.Diagnostics;

namespace Microsoft.Xunit.Performance
{
    partial class InstructionsRetiredMetricDiscoverer : IPerformanceMetricDiscoverer
    {
        class InstructionsRetiredEvaluator : PerformanceMetricEvaluator
        {
            PerformanceMetricEvaluationContext _context;
            long _count;
            int _interval;
            int _profileSource;

            public override void BeginIteration(TraceEvent beginEvent) => _count = 0;

            public override double EndIteration(TraceEvent endEvent) => _count;

            internal void Initialize(PerformanceMetricEvaluationContext context, int profileSource)
            {
                lock (this)
                {
                    if (_context == null)
                    {
                        context.TraceEventSource.Kernel.PerfInfoCollectionStart += PerfInfoCollectionStart;
                        context.TraceEventSource.Kernel.PerfInfoPMCSample += PerfInfoPMCSample;
                        _context = context;
                        _profileSource = profileSource;
                    }
                    else
                    {
                        // FIXME: We should fail here instead.
                        Debug.Assert(_context == context);
                        Debug.Assert(_profileSource == profileSource);
                    }
                }
            }

            void PerfInfoCollectionStart(SampledProfileIntervalTraceData ev)
            {
                if (ev.SampleSource == _profileSource)
                    _interval = ev.NewInterval;
            }

            void PerfInfoPMCSample(PMCCounterProfTraceData ev)
            {
                if (ev.ProfileSource == _profileSource && _context.IsTestEvent(ev))
                    _count += _interval;
            }
        }
    }
}