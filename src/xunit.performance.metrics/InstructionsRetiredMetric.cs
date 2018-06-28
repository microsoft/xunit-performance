// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.Xunit.Performance
{
    partial class InstructionsRetiredMetricDiscoverer : IPerformanceMetricDiscoverer
    {
        internal class InstructionsRetiredMetric : PerformanceMetric
        {
            //
            // We create just one PerformanceMetricEvaluator per PerformanceEvaluationContext (which represents a single ETW session).
            // This lets us track state (the counter sampling interval) across test cases.  It would be nice if PerformanceMetricEvaluationContext
            // made this easier, but for now we'll just track the relationship with a ConditionalWeakTable.
            //
            // TODO: consider better support for persistent state in PerformanceMetricEvaluationContext.
            //
            static readonly ConditionalWeakTable<PerformanceMetricEvaluationContext, InstructionsRetiredEvaluator> s_evaluators =
                new ConditionalWeakTable<PerformanceMetricEvaluationContext, InstructionsRetiredEvaluator>();

            readonly int _interval;
            readonly int _profileSource;

            public InstructionsRetiredMetric()
                : base(CounterName, "Instructions Retired", PerformanceMetricUnits.Count)
            {
                _interval = DefaultInterval;
                _profileSource = GetProfileSourceInfoId(CounterName);
            }

            public InstructionsRetiredMetric(int interval, int profileSource)
                : base(CounterName, "Instructions Retired", PerformanceMetricUnits.Count)
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
                        Keywords = unchecked((ulong)KernelTraceEventParser.Keywords.PMCProfile),
                        StackKeywords = unchecked((ulong)KernelTraceEventParser.Keywords.PMCProfile),
                    };
                    yield return new CpuCounterInfo()
                    {
                        CounterName = CounterName,
                        Interval = _interval,
                    };
                }
            }

            public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context)
            {
                var evaluator = s_evaluators.GetOrCreateValue(context);
                evaluator.Initialize(context, _profileSource);
                return evaluator;
            }
        }
    }
}