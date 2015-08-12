using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Xunit.Performance
{
    static class BenchmarkConfiguration
    {
        public static readonly string RunId = Environment.GetEnvironmentVariable("XUNIT_PERFORMANCE_RUN_ID") ?? Environment.MachineName + ":" + DateTimeOffset.UtcNow.ToString("u");
        public static readonly int MaxIteration = int.Parse(Environment.GetEnvironmentVariable("XUNIT_PERFORMANCE_MAX_ITERATION") ?? "1000");
        public static readonly int MaxTotalMilliseconds = int.Parse(Environment.GetEnvironmentVariable("XUNIT_PERFORMANCE_MAX_TOTAL_MILLISECONDS") ?? "1000");
    }
}
