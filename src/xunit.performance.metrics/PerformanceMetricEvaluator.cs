using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Sdk
{
    public abstract class PerformanceMetricEvaluator : IDisposable
    {
        public abstract void BeginIteration(TraceEvent beginEvent);

        public abstract IEnumerable<PerformanceMetricValue> EndIteration(TraceEvent endEvent);

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
