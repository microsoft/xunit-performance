namespace Microsoft.Xunit.Performance.Api
{
    /// <summary>
    /// The result of a ShellOut completing.
    /// </summary>
    internal class XunitPerformanceProcessResult
    {
        /// <summary>
        /// The path to the executable that was run.
        /// </summary>
        public string ExecutablePath { get; set; }

        /// <summary>
        /// The arguments that were passed to the process.
        /// </summary>
        public string Args { get; set; }

        /// <summary>
        /// The exit code of the process.
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// The entire standard-out of the process.
        /// </summary>
        public string StdOut { get; set; }

        /// <summary>
        /// The entire standard-error of the process.
        /// </summary>
        public string StdErr { get; set; }

        /// <summary>
        /// True if the command returned an exit code other
        /// than zero.
        /// </summary>
        public bool Failed => Code != 0;

        /// <summary>
        /// True if the command returned an exit code of 0.
        /// </summary>
        public bool Succeeded => !Failed;
    }
}
