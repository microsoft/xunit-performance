namespace Microsoft.Xunit.Performance.Api
{
    internal sealed class BranchMispredictionsPerformanceMonitorCounter : IPerformanceMonitorCounter
    {
        public string DisplayName => "Branch Mispredictions";

        public string Name => "BranchMispredictions";

        public string Unit => "count";

        public int Interval => 1000;
    }
}
