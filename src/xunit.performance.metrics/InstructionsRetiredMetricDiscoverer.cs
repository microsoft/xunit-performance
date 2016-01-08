// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance
{
    internal class InstructionsRetiredMetricDiscoverer : IPerformanceMetricDiscoverer
    {
        const int DefaultInterval = 100000; // Instructions per event.
        const string CounterName = "InstructionRetired";

        readonly int _profileSource;

        public InstructionsRetiredMetricDiscoverer()
        {
            ProfileSourceInfo info;
            if (TraceEventProfileSources.GetInfo().TryGetValue(CounterName, out info))
                _profileSource = info.ID;
            else
                _profileSource = -1;
        }

        public IEnumerable<PerformanceMetricInfo> GetMetrics(IAttributeInfo metricAttribute)
        {
            if (_profileSource != -1)
            {
                var interval = (int)(metricAttribute.GetConstructorArguments().FirstOrDefault() ?? DefaultInterval);
                yield return new InstructionsRetiredMetric(interval, _profileSource);
            }
        }

        private class InstructionsRetiredMetric : PerformanceMetric
        {
            private readonly int _interval;
            private readonly int _profileSource;

            public InstructionsRetiredMetric(int interval, int profileSource)
                : base("InstRetired", "Instructions Retired", PerformanceMetricUnits.Count)
            {
                _interval = interval;
                _profileSource = profileSource;
            }

            public override IEnumerable<ProviderInfo> ProviderInfo
            {
                get
                {
                    yield return new KernelProviderInfo()
                    {
                        Keywords = unchecked((ulong)(KernelTraceEventParser.Keywords.PMCProfile | KernelTraceEventParser.Keywords.Profile)),
                    };
                    yield return new CpuCounterInfo()
                    {
                        CounterName = CounterName,
                        Interval = _interval,
                    };
                }
            }

            //
            // We create just one PerformanceMetricEvaluator per PerformanceEvaluationContext (which represents a single ETW session).
            // This lets us track state (the counter sampling interval) across test cases.  It would be nice if PerformanceMetricEvaluationContext
            // made this easier, but for now we'll just track the relationship with a ConditionalWeakTable.
            //
            // TODO: consider better support for persistent state in PerformanceMetricEvaluationContext.
            //
            private static ConditionalWeakTable<PerformanceMetricEvaluationContext, InstructionsRetiredEvaluator> s_evaluators = 
                new ConditionalWeakTable<PerformanceMetricEvaluationContext, InstructionsRetiredEvaluator>();

            public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context)
            {
                var evaluator = s_evaluators.GetOrCreateValue(context);
                evaluator.Initialize(context, _profileSource);
                return evaluator;
            }
        }

        private class InstructionsRetiredEvaluator : PerformanceMetricEvaluator
        {
            private PerformanceMetricEvaluationContext _context;
            private int _profileSource;
            private int _interval;
            private long _count;

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
                        Debug.Assert(_context == context);
                        Debug.Assert(_profileSource == profileSource);
                    }
                }
            }

            private void PerfInfoCollectionStart(SampledProfileIntervalTraceData ev)
            {
                if (ev.SampleSource == _profileSource)
                    _interval = ev.NewInterval;
            }

            private void PerfInfoPMCSample(PMCCounterProfTraceData ev)
            {
                if (ev.ProfileSource == _profileSource && _context.IsTestEvent(ev))
                    _count += _interval;
            }

            public override void BeginIteration(TraceEvent beginEvent)
            {
                _count = 0;
            }

            public override object EndIteration(TraceEvent endEvent)
            {
                return _count;
            }
        }
    }
}
