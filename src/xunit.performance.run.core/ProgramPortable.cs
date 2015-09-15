using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xunit.Performance.Sdk;

namespace Microsoft.Xunit.Performance
{
    public class ProgramPortable : ProgramCore
    {
        public static int Main(string[] args)
        {
            return new ProgramPortable().Run(args);
        }

        protected override IPerformanceMetricReader GetPerformanceMetricReader(IEnumerable<PerformanceTestInfo> tests, string etlPath, string runId)
        {
            throw new NotImplementedException();
        }

        protected override string GetRuntimeVersion()
        {
            return "Portable";
        }

        protected override IDisposable StartTracing(IEnumerable<PerformanceTestInfo> tests, string pathBase)
        {
            Environment.SetEnvironmentVariable("XUNIT_PERFORMANCE_FILE_LOG_PATH", pathBase + ".log");
            return null;
        }
    }
}
