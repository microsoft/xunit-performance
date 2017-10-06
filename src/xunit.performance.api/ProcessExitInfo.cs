using System;
using System.Diagnostics;

namespace Microsoft.Xunit.Performance.Api
{
    /// <summary>
    /// Interface that captures the state of the run scenario.
    /// </summary>
    public sealed class ProcessExitInfo
    {
        /// <summary>
        /// Initializes a new instance of a ProcessExitInfo class with the run process.
        /// </summary>
        /// <param name="process"></param>
        public ProcessExitInfo(Process process)
        {
            if (process == null)
                throw new ArgumentNullException(nameof(process));
            if (!process.HasExited)
                throw new InvalidOperationException($"{process.ProcessName} has not exited.");

            ProcessId = process.Id;
            ExitCode = process.ExitCode;
            StartTime = process.StartTime;
            ExitTime = process.ExitTime;
        }

        /// <summary>
        /// Gets the scenario exit code.
        /// </summary>
        public int ExitCode { get; }

        /// <summary>
        /// Gets the unique identifier that was assigned to the process.
        /// </summary>
        public int ProcessId { get; }

        /// <summary>
        /// Gets the time that the associated process was started.
        /// </summary>
        public DateTime StartTime { get; }

        /// <summary>
        /// Gets the time that the associated process exited.
        /// </summary>
        public DateTime ExitTime { get; }
    }
}
