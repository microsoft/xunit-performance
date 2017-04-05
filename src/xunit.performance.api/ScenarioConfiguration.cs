using System;

namespace Microsoft.Xunit.Performance.Api
{
    /// <summary>
    /// Provides an interface to configure how benchmark scenarios are run by the XunitPerformanceHarness.
    /// </summary>
    public sealed class ScenarioConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the ScenarioConfiguration class.
        /// </summary>
        /// <param name="milliseconds">The amount of time, in milliseconds, to wait for one iteration process to exit.</param>
        /// <param name="iterations">Number of times a benchmark scenario process will be executed.</param>
        public ScenarioConfiguration(double milliseconds, int iterations = 10)
        {
            if (milliseconds <= 0)
                throw new InvalidOperationException("The time out per iteration must be a positive number.");
            if (iterations <= 1)
                throw new InvalidOperationException("The number of iterations must be greater than 1.");

            Iterations = iterations;
            TimeoutPerIteration = TimeSpan.FromMilliseconds(milliseconds);
        }

        /// <summary>
        /// Number of times a benchmark scenario process will be executed.
        /// </summary>
        public int Iterations { get; }

        /// <summary>
        /// The amount of time to wait for one iteration process to exit.
        /// </summary>
        public TimeSpan TimeoutPerIteration { get; }
    }
}
