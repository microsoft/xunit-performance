using Microsoft.Xunit.Performance.Sdk;
using System.Runtime.CompilerServices;

namespace Microsoft.Xunit.Performance.Api
{
    sealed class GenericPerformanceMonitorCounterMetric<T> : BasePerformanceMonitorCounter
        where T : IPerformanceMonitorCounter, new()
    {
        //
        // We create just one PerformanceMetricEvaluator per PerformanceEvaluationContext (which represents a single ETW session).
        // This lets us track state (the counter sampling interval) across test cases.  It would be nice if PerformanceMetricEvaluationContext
        // made this easier, but for now we'll just track the relationship with a ConditionalWeakTable.
        //
        // TODO: consider better support for persistent state in PerformanceMetricEvaluationContext.
        //
        static readonly ConditionalWeakTable<PerformanceMetricEvaluationContext, GenericPerformanceMonitorCounterEvaluator<T>> s_evaluators;

        static GenericPerformanceMonitorCounterMetric() => s_evaluators = new ConditionalWeakTable<PerformanceMetricEvaluationContext, GenericPerformanceMonitorCounterEvaluator<T>>();

        public GenericPerformanceMonitorCounterMetric(T pmc) : base(pmc)
        {
        }

        public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context)
        {
            var evaluator = s_evaluators.GetOrCreateValue(context);
            evaluator.Initialize(context, ProfileSourceInfoID);
            return evaluator;
        }
    }
}