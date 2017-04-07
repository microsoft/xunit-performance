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
        public ScenarioConfiguration(TimeSpan timeSpan)
        {
            if (timeSpan.TotalMilliseconds < 1)
                throw new InvalidOperationException("The time out per iteration must be a positive number.");

            Iterations = 10;
            TimeoutPerIteration = timeSpan;
            SuccessExitCodes = new[] { 0 };
        }

        /// <summary>
        /// Initializes a new instance of the ScenarioConfiguration class (deep copy).
        /// </summary>
        /// <param name="scenarioConfiguration">An instance of the ScenarioConfiguration class</param>
        public ScenarioConfiguration(ScenarioConfiguration scenarioConfiguration)
        {
            Iterations = scenarioConfiguration.Iterations;
            TimeoutPerIteration = scenarioConfiguration.TimeoutPerIteration;
            SuccessExitCodes = scenarioConfiguration.SuccessExitCodes;
        }

        /// <summary>
        /// Number of times a benchmark scenario process will be executed.
        /// </summary>
        /// <remarks>
        /// If the specified number of iterations is greater than 1, then we
        /// will consider the first iteration as a warm-up and discard its
        /// result when computing the statistics.
        /// </remarks>
        public int Iterations
        {
            get => _iterations;

            set
            {
                if (value < 1)
                    throw new InvalidOperationException("The number of iterations must be greater than 0.");
                _iterations = value;
            }
        }

        /// <summary>
        /// The amount of time to wait for one iteration process to exit.
        /// </summary>
        public TimeSpan TimeoutPerIteration { get; }

        /// <summary>
        /// Exit codes that indicates success when the benchmark scenario process terminates.
        /// If this parameter is not specified, then the only valid exit code will 0.
        /// </summary>
        public IEnumerable<int> SuccessExitCodes
        {
            get => _successExitCodes;

            set
            {
                if (value == null)
                    throw new ArgumentNullException($"Assigned a null collection to {nameof(SuccessExitCodes)}.");
                if (value.Count() == 0)
                    throw new InvalidOperationException($"Assigned an empty collection to {nameof(SuccessExitCodes)}");

                _successExitCodes = value.ToArray();
            }
        }

        private int _iterations;
        private IEnumerable<int> _successExitCodes;
    }
}
