using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Xunit.Performance
{
    /// <summary>
    /// Allows a performance test to manually control test iterations.
    /// </summary>
    public class BenchmarkIterationControl
    {
        /// <summary>
        /// Returns true if the test should execute at least one more iteration.
        /// </summary>
        public bool NeedMoreIterations { get; }

        /// <summary>
        /// Provides a <see cref="IterationCancellationToken"/> that will enter the cancelled state when no more iterations are needed.
        /// </summary>
        public CancellationToken IterationCancellationToken { get; }

        /// <summary>
        /// Called by the test method to indicate that an iteration is starting.
        /// </summary>
        /// <remarks>
        /// When the iteration is complete, call <see cref="RunningBenchmarkIteration.Dispose"/>.
        /// </remarks>
        /// <returns>A <see cref="RunningBenchmarkIteration"/> representing this iteration.</returns>
        /// <param name="iteration">The ordinal of this iteration.  If zero, this is a "warmup" iteration that should be ignored by analysis.</param>
        public RunningBenchmarkIteration StartIteration(int iteration)
        {
            return new RunningBenchmarkIteration();
        }
    }

    /// <summary>
    /// Represents a benchmark iteration that is currently executing.
    /// </summary>
    /// <remarks>
    /// Call <see cref="Dispose"/> when the iteration is complete.
    /// </remarks>
    public struct RunningBenchmarkIteration : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
        }
    }
}
