using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Sdk
{
    public abstract class PerformanceMetricEvaluator
    {
        public abstract void TestIterationStarted();

        public abstract IEnumerable<PerformanceMetricValue> TestIterationFinished();
    }
}
