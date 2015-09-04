using Microsoft.Xunit.Performance.Internal;

namespace Microsoft.Xunit.Performance
{
    /// <summary>
    /// Represents a single iteration of a benchmark.
    /// </summary>
    public struct BenchmarkIteration
    {
        private readonly BenchmarkIterator _iterator;
        private readonly int _iterationNumber;

        internal BenchmarkIteration(BenchmarkIterator iterator, int iterationNumber)
        {
            _iterator = iterator;
            _iterationNumber = iterationNumber;
        }

        /// <summary>
        /// Starts measuring performance metrics for this iteration.
        /// </summary>
        /// <returns></returns>
        public BenchmarkIterationMeasurement StartMeasurement()
        {
            _iterator.StartMeasurement(_iterationNumber);
            return new BenchmarkIterationMeasurement(this);
        }

        internal void StopMeasurement()
        {
            _iterator.StopMeasurement(_iterationNumber);
        }
    }
}
