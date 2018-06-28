// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing;
using Microsoft.Xunit.Performance.Sdk;

namespace Microsoft.Xunit.Performance
{
    partial class BenchmarkMetricDiscoverer : IPerformanceMetricDiscoverer
    {
        internal class BenchmarkDurationEvaluator : PerformanceMetricEvaluator
        {
            double _iterationStartRelativeMSec;

            public BenchmarkDurationEvaluator(PerformanceMetricEvaluationContext context)
            {
            }

            public override void BeginIteration(TraceEvent beginEvent) => _iterationStartRelativeMSec = beginEvent.TimeStampRelativeMSec;

            public override double EndIteration(TraceEvent endEvent) => endEvent.TimeStampRelativeMSec - _iterationStartRelativeMSec;
        }
    }
}