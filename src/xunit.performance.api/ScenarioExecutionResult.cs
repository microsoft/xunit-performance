// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance.Api.Profilers.Etw;
using System;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Api
{
    /// <summary>
    /// Defines an interface for a performance scenario result.
    /// </summary>
    public sealed class ScenarioExecutionResult
    {
        /// <summary>
        /// Initializes a new instance of the ScenarioExecutionResult class.
        /// </summary>
        /// <param name="process">Scenario benchmark process that was run.</param>
        /// <param name="startTime">The time that the associated process was started.</param>
        /// <param name="exitTime">The time that the associated process exited.</param>
        /// <param name="configuration">Configuration for the scenario that was run.</param>
        internal ScenarioExecutionResult(System.Diagnostics.Process process, DateTime startTime, DateTime exitTime, ScenarioTestConfiguration configuration)
        {
            ProcessExitInfo = new ProcessExitInfo(process, startTime, exitTime);
            Configuration = configuration;
        }

        /// <summary>
        /// Configuration for the scenario that was run
        /// </summary>
        public ScenarioTestConfiguration Configuration { get; }

        /// <summary>
        /// Binary .etl file name where the ETW traces were logged.
        /// </summary>
        public string EventLogFileName { get; set; }

        /// <summary>
        /// This is the list of Pmc that were captured by the ETW event listener.
        /// </summary>
        public ISet<PerformanceMonitorCounter> PerformanceMonitorCounters { get; set; }

        /// <summary>
        /// State of the run scenario process.
        /// </summary>
        public ProcessExitInfo ProcessExitInfo { get; }
    }
}