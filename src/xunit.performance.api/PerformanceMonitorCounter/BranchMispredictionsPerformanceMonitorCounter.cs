namespace Microsoft.Xunit.Performance.Api
{
    sealed class BranchMispredictionsPerformanceMonitorCounter : IPerformanceMonitorCounter
    {
        public string DisplayName => "Branch Mispredictions";

        public int Interval => 1000;
        public string Name => "BranchMispredictions";

        public string Unit => "count";
    }
}