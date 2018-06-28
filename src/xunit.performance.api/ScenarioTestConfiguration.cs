using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Xunit.Performance.Api
{
    /// <summary>
    /// Provides an interface to configure how benchmark scenarios are run by the XunitPerformanceHarness.
    /// </summary>
    public sealed class ScenarioTestConfiguration
    {
        int _iterations;

        IEnumerable<int> _successExitCodes;

        /// <summary>
        /// Initializes a new instance of the ScenarioConfiguration class.
        /// </summary>
        /// <param name="timeoutPerIteration">The amount of time to wait for one iteration process to exit.</param>
        /// <param name="startInfo">The data with which to start the scenario.</param>
        public ScenarioTestConfiguration(TimeSpan timeoutPerIteration, ProcessStartInfo startInfo)
        {
            if (timeoutPerIteration.TotalMilliseconds < 1)
                throw new InvalidOperationException("The time out per iteration must be a positive number.");

            Iterations = 10;
            StartInfo = startInfo ?? throw new ArgumentNullException(nameof(startInfo));
            TimeoutPerIteration = timeoutPerIteration;
            SuccessExitCodes = new[] { 0 };
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
        /// The action that will be executed after every benchmark scenario execution.
        /// </summary>
        public Action<ScenarioExecutionResult> PostIterationDelegate { get; set; }

        /// <summary>
        /// The action that will be executed before every benchmark scenario execution.
        /// </summary>
        public Action<ScenarioTest> PreIterationDelegate { get; set; }

        /// <summary>
        /// Indicates whether the results from the test should be saved after it is run
        /// </summary>
        public bool SaveResults { get; set; } = true;

        /// <summary>
        /// The scenario to which this test belongs
        /// </summary>
        public ScenarioBenchmark Scenario { get; set; }

        /// <summary>
        /// The data with which to start the scenario.
        /// </summary>
        public ProcessStartInfo StartInfo { get; }

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
                    throw new ArgumentNullException(nameof(SuccessExitCodes));
                if (!value.Any())
                    throw new InvalidOperationException($"Assigned an empty collection to {nameof(SuccessExitCodes)}");

                _successExitCodes = value.ToArray();
            }
        }

        /// <summary>
        /// The name of the test
        /// </summary>
        public string TestName { get; set; }

        /// <summary>
        /// The amount of time to wait for one iteration process to exit.
        /// </summary>
        public TimeSpan TimeoutPerIteration { get; }
    }
}