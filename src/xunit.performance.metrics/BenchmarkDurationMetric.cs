using Microsoft.Xunit.Performance.Sdk;

namespace Microsoft.Xunit.Performance
{
    class BenchmarkDurationMetric : PerformanceMetric
    {
        public BenchmarkDurationMetric() : base("Duration", "msec", PerformanceMetricInterpretation.LowerIsBetter) { }

        public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context)
        {
            return new BenchmarkDurationEvaluator(context);
        }
    }
}
