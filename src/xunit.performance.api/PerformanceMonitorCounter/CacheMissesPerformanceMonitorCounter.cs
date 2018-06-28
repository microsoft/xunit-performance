namespace Microsoft.Xunit.Performance.Api
{
    sealed class CacheMissesPerformanceMonitorCounter : IPerformanceMonitorCounter
    {
        public string DisplayName => "Cache Misses";

        public int Interval => 1000;
        public string Name => "CacheMisses";

        public string Unit => "count";
    }
}