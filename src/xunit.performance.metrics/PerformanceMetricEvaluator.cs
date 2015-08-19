using System;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Sdk
{
    public abstract class PerformanceMetricEvaluator : IDisposable
    {
        public abstract void BeginIteration();

        public abstract IEnumerable<PerformanceMetricValue> EndIteration();

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        ~PerformanceMetricEvaluator()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
