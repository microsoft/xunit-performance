using System;

namespace Microsoft.Xunit.Performance
{
    static class BenchmarkConfiguration
    {
        public static readonly string RunId = Environment.GetEnvironmentVariable("XUNIT_PERFORMANCE_RUN_ID");
        public static readonly int MaxIteration = int.Parse(Environment.GetEnvironmentVariable("XUNIT_PERFORMANCE_MAX_ITERATION") ?? "0");
        public static readonly int MaxTotalMilliseconds = int.Parse(Environment.GetEnvironmentVariable("XUNIT_PERFORMANCE_MAX_TOTAL_MILLISECONDS") ?? "0");
    }
}
