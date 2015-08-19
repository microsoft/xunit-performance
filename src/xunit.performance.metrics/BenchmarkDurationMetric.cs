using Microsoft.Xunit.Performance.Sdk;

namespace Microsoft.Xunit.Performance
{
    class BenchmarkDurationMetric : PerformanceMetric
    {
        public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context)
        {
            return new BenchmarkMetricEvaluator(context);
        }
    }
}
