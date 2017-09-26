using System;

namespace Microsoft.Xunit.Performance
{
    /// <summary>
    /// Represents an ongoing measurement of a benchmark iteration.
    /// </summary>
    public struct BenchmarkIterationMeasurement : IDisposable
    {
        private readonly BenchmarkIteration _iteration;

        internal BenchmarkIterationMeasurement(BenchmarkIteration iteration)
        {
            _disposedValue = false;
            _iteration = iteration;
        }

        #region IDisposable Support
        private bool _disposedValue;

        void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // Completes measurement of this iteration.
                    _iteration.StopMeasurement();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
