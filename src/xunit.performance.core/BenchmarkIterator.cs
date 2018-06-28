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
        static SemaphoreSlim s_semaphore = new SemaphoreSlim(1);

        protected BenchmarkIterator(long innerIterations) => InnerIterationCount = innerIterations;

        internal static BenchmarkIterator Current { get; private set; }

        /// <summary>
        /// Gets the inner iteration count for the current benchmark
        /// </summary>
        internal long InnerIterationCount { get; }

        /// <summary>
        /// Gets the iterations for the current benchmark
        /// </summary>
        protected internal abstract IEnumerable<BenchmarkIteration> Iterations { get; }

        /// <summary>
        /// Runs the specified method with this <see cref="BenchmarkIterator"/> as the <see cref="Current"/> iterator.
        /// </summary>
        /// <param name="testMethod"></param>
        /// <returns></returns>
        public async Task RunAsync(Func<Task> testMethod)
        {
            //
            // Ensure there's only one "current" iterator at a time.
            //
            await s_semaphore.WaitAsync();
            try
            {
                //
                // Prevent neseted iterators
                //
                if (Current != null)
                    throw new InvalidOperationException();

                //
                // Set the current iterator, and call the test.
                //
                Current = this;
                await testMethod();
            }
            finally
            {
                Current = null;
                s_semaphore.Release();
            }
        }

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