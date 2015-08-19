using System;
using System.Collections.Generic;
using Microsoft.Xunit.Performance.Sdk;
using Microsoft.Diagnostics.Tracing;

namespace Microsoft.Xunit.Performance
{
    internal class BenchmarkDurationEvaluator : PerformanceMetricEvaluator
    {
        double _iterationStartRelativeMSec;

        public BenchmarkDurationEvaluator(PerformanceMetricEvaluationContext context)
        {
        }

        public override void BeginIteration(TraceEvent beginEvent)
        {
            _iterationStartRelativeMSec = beginEvent.TimeStampRelativeMSec;
        }

        public override double EndIteration(TraceEvent endEvent)
        {
            return endEvent.TimeStampRelativeMSec - _iterationStartRelativeMSec;
        }
    }
}