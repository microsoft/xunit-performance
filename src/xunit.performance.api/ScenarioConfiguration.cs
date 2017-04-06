using System;
using System.Collections.Generic;
using System.Linq;

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
        /// <param name="timeSpan">The amount of time to wait for one iteration process to exit.</param>
        /// <param name="iterations">Number of times a benchmark scenario process will be executed.</param>
        /// <param name="validExitCodes">
        /// A collection of exit codes that indicates success when the benchmark scenario process terminates.
        /// If this parameter is not specified, then the only valid exit code will 0.
        /// </param>
        public ScenarioConfiguration(TimeSpan timeSpan, int iterations = 10, IEnumerable<int> validExitCodes = null)
        {
            if (timeSpan.TotalMilliseconds <= 0)
                throw new InvalidOperationException("The time out per iteration must be a positive number.");
            if (iterations <= 1)
                throw new InvalidOperationException("The number of iterations must be greater than 1.");

            Iterations = iterations;
            TimeoutPerIteration = timeSpan;
            ValidExitCodes = validExitCodes != null ? new List<int>(validExitCodes) : new List<int> { 0 };
        }

        /// <summary>
        /// Number of times a benchmark scenario process will be executed.
        /// </summary>
        public int Iterations { get; }

        /// <summary>
        /// The amount of time to wait for one iteration process to exit.
        /// </summary>
        public TimeSpan TimeoutPerIteration { get; }

        /// <summary>
        /// Exit codes that indicates success when the benchmark scenario process terminates.
        /// </summary>
        public IEnumerable<int> ValidExitCodes { get; }
    }
}
