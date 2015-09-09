// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance
{
    internal class InstructionsRetiredMetricDiscoverer : IPerformanceMetricDiscoverer
    {
        const int DefaultInterval = 100000; // Instructions per event.

        public IEnumerable<PerformanceMetric> GetMetrics(IAttributeInfo metricAttribute)
        {
            var interval = (int)(metricAttribute.GetConstructorArguments().FirstOrDefault() ?? DefaultInterval);
            yield return new InstructionsRetiredMetric(interval);
        }

        private class InstructionsRetiredMetric : PerformanceMetric
        {
            private readonly int _interval;

            public InstructionsRetiredMetric(int interval)
                : base("InstRetired", "Instructions Retired", PerformanceMetricUnits.Count)
            {
                _interval = interval;
            }

            public override IEnumerable<ProviderInfo> ProviderInfo
            {
                get
                {
                    yield return new KernelProviderInfo()
                    {
                        Keywords = unchecked((ulong)KernelTraceEventParser.Keywords.PMCProfile),
                        StackKeywords = unchecked((ulong)KernelTraceEventParser.Keywords.PMCProfile)
                    };
                    yield return new CpuCounterInfo()
                    {
                        CounterName = "InstructionRetired",
                        Interval = _interval,
                    };
                }
            }

            public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context)
            {
                return new InstructionsRetiredEvaluator(context, _interval);
            }
        }

        private class InstructionsRetiredEvaluator : PerformanceMetricEvaluator
        {
            private readonly PerformanceMetricEvaluationContext _context;
            private int _interval;
            private long _count;

            public InstructionsRetiredEvaluator(PerformanceMetricEvaluationContext context, int interval)
            {
                _context = context;
                _interval = interval;
                _context.TraceEventSource.Kernel.PerfInfoSetInterval += PerfInfoSetInterval;
                _context.TraceEventSource.Kernel.PerfInfoPMCSample += PerfInfoPMCSample;
            }

            private void PerfInfoSetInterval(SampledProfileIntervalTraceData ev)
            {
                _interval = ev.NewInterval;
            }

            private void PerfInfoPMCSample(PMCCounterProfTraceData ev)
            {
                if (_context.IsTestEvent(ev))
                    _count += _interval;
            }

            public override void BeginIteration(TraceEvent beginEvent)
            {
                _count = 0;
            }

            public override double EndIteration(TraceEvent endEvent)
            {
                return _count;
            }
        }
    }
}
