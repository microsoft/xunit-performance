// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Xunit.Performance.Sdk;
using Xunit.Abstractions;
using Microsoft.Diagnostics.Tracing;

namespace Microsoft.Xunit.Performance
{
    internal class BenchmarkMetricDiscoverer : IPerformanceMetricDiscoverer
    {
        public IEnumerable<PerformanceMetricInfo> GetMetrics(IAttributeInfo metricAttribute)
        {
            yield return new BenchmarkDurationMetric();
        }

        private class BenchmarkDurationMetric : PerformanceMetric
        {
            public BenchmarkDurationMetric()
                : base("Duration", "Duration", PerformanceMetricUnits.Milliseconds, PerformanceMetricBetterDirections.Descending)
            {
            }

            public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context)
            {
                return new BenchmarkDurationEvaluator(context);
            }
        }
        internal class BenchmarkDurationEvaluator : PerformanceMetricEvaluator
        {
            private double _iterationStartRelativeMSec;

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
