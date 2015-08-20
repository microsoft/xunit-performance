using System;
using System.Collections.Generic;
using Microsoft.Xunit.Performance.Sdk;
using Xunit.Abstractions;
using Microsoft.Diagnostics.Tracing;

namespace Microsoft.Xunit.Performance
{
    class BenchmarkMetricDiscoverer : IPerformanceMetricDiscoverer
    {
        public IEnumerable<PerformanceMetric> GetMetrics(IAttributeInfo metricAttribute)
        {
            yield return new BenchmarkDurationMetric();
        }

        class BenchmarkDurationMetric : PerformanceMetric
        {
            public BenchmarkDurationMetric() 
                : base("Duration", "msec", PerformanceMetricInterpretation.LowerIsBetter)
            {
            }

            public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context)
            {
                return new BenchmarkDurationEvaluator(context);
            }
        }
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
}
