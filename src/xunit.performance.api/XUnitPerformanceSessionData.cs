using System;

namespace Microsoft.Xunit.Performance.Api
{
    internal struct XUnitPerformanceSessionData
    {
        public string AssemblyFileName { get; set; }

        public Action<string> CollectOutputFilesCallback { get; set; }

        public string OutputDirectory { get; set; }

        public string RunId { get; set; }
    }
}
