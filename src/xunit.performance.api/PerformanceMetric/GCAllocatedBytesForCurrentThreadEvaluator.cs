using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.MicrosoftXunitBenchmark;
using Microsoft.Xunit.Performance.Execution;
using Microsoft.Xunit.Performance.Sdk;

namespace Microsoft.Xunit.Performance.Api
{
    internal class GCAllocatedBytesForCurrentThreadEvaluator : PerformanceMetricEvaluator
    {
        public GCAllocatedBytesForCurrentThreadEvaluator()
        {
        }

        public override void BeginIteration(TraceEvent traceEvent)
        {
            _bytesBeforeIteration = ((BenchmarkIterationStartArgs)traceEvent).AllocatedBytes;
        }

        public override double EndIteration(TraceEvent traceEvent)
        {
            var bytesAfterIteration = ((BenchmarkIterationStopArgs)traceEvent).AllocatedBytes;
            return AllocatedBytesForCurrentThread.GetTotalAllocatedBytes(
                _bytesBeforeIteration, bytesAfterIteration);
        }

        private long _bytesBeforeIteration;
    }
}
