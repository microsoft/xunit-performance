namespace Microsoft.Xunit.Performance.Api
{
    internal sealed class CacheMissesPerformanceMonitorCounter : IPerformanceMonitorCounter
    {
        public string DisplayName => "Cache Misses";

        public string Name => "CacheMisses";

        public string Unit => "count";

        public int Interval => 1000;
    }
}
