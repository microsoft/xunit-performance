// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance.Sdk;

namespace Microsoft.Xunit.Performance
{
    partial class BenchmarkMetricDiscoverer : IPerformanceMetricDiscoverer
    {
        internal class BenchmarkDurationMetric : PerformanceMetric
        {
            public BenchmarkDurationMetric()
                : base("Duration", "Duration", PerformanceMetricUnits.Milliseconds)
            {
            }

            public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context) => new BenchmarkDurationEvaluator(context);
        }
    }
}