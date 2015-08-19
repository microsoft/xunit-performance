using System;
using System.Collections.Generic;
using Microsoft.Xunit.Performance.Sdk;
using Microsoft.Diagnostics.Tracing;

namespace Microsoft.Xunit.Performance
{
    internal class BenchmarkMetricEvaluator : PerformanceMetricEvaluator
    {
        double _iterationStartRelativeMSec;

        public BenchmarkMetricEvaluator(PerformanceMetricEvaluationContext context)
        {
        }

        public override void BeginIteration(TraceEvent beginEvent)
        {
            _iterationStartRelativeMSec = beginEvent.TimeStampRelativeMSec;
        }

        public override IEnumerable<PerformanceMetricValue> EndIteration(TraceEvent endEvent)
        {
            var iterationEndRelativeMSec = endEvent.TimeStampRelativeMSec;
            yield return new PerformanceMetricValue("Duration", "msec", iterationEndRelativeMSec - _iterationStartRelativeMSec);
        }
    }
}