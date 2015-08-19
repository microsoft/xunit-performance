using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Sdk
{
    public abstract class PerformanceMetricEvaluator
    {
        public abstract void BeginIteration();

        public abstract IEnumerable<PerformanceMetricValue> EndIteration();
    }
}
