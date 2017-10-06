using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Xunit.Performance.Api
{
    public sealed class ScenarioExecutionResult
    {
        public ScenarioExecutionResult(Process process)
        {
            ProcessExitInfo = new ProcessExitInfo(process);
        }

        public ProcessExitInfo ProcessExitInfo { get; }

        public string EventLogFileName { get; set; }

        /// <summary>
        /// This is the list of Pmc that were captured by the ETW event listener.
        /// TODO: We should reconsider this type, as it is internal data and it does not contain the display name.
        /// </summary>
        public IEnumerable<PerformanceMonitorCounter> PerformanceMonitorCounters { get; set; }
    }
}
