using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Xunit.Performance.Internal
{
    /// <summary>
    /// Provides iterations for <see cref="Benchmark.Iterations"/>
    /// </summary>
    public abstract class BenchmarkIterator
    {
        internal static BenchmarkIterator Current { get; private set; }

        private static SemaphoreSlim s_semaphore = new SemaphoreSlim(1);

        /// <summary>
        /// Runs the specified method with this <see cref="BenchmarkIterator"/> as the <see cref="Current"/> iterator.
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public async Task RunAsync(Func<Task> method)
        {
            await s_semaphore.WaitAsync();
            try
            {
                if (Current != null)
                    throw new InvalidOperationException();

                Current = this;
                await method();
            }
            finally
            {
                Current = null;
                s_semaphore.Release();
            }
        }

        /// <summary>
        /// Gets the iterations for the current benchmark
        /// </summary>
        protected internal abstract IEnumerable<BenchmarkIteration> Iterations { get; }

        /// <summary>
        /// Starts measurement for the given iteration, if it is the current iteration.
        /// </summary>
        /// <param name="iterationNumber"></param>
        protected internal abstract void StartMeasurement(int iterationNumber);

        /// <summary>
        /// Stops measurement for the given iteration, if it is the current iteration.
        /// </summary>
        /// <param name="iterationNumber"></param>
        protected internal abstract void StopMeasurement(int iterationNumber);

        /// <summary>
        /// Creates a new <see cref="BenchmarkIteration"/> instance for the given iteration.
        /// </summary>
        /// <param name="iterationNumber"></param>
        /// <returns></returns>
        protected BenchmarkIteration CreateIteration(int iterationNumber) => new BenchmarkIteration(this, iterationNumber);
    }
}
