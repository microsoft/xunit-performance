using System;

namespace Microsoft.Xunit.Performance
{
    /// <summary>
    /// Represents an ongoing measurement of a benchmark iteration.
    /// </summary>
    public struct BenchmarkIterationMeasurement : IDisposable
    {
        readonly BenchmarkIteration _iteration;

        internal BenchmarkIterationMeasurement(BenchmarkIteration iteration) => _iteration = iteration;

        /// <summary>
        /// Completes measurement of this iteration.
        /// </summary>
        public void Dispose() => _iteration.StopMeasurement();
    }
}