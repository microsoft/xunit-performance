namespace Microsoft.Xunit.Performance.Api
{
    sealed class InstructionRetiredPerformanceMonitorCounter : IPerformanceMonitorCounter
    {
        public string DisplayName => "Instructions Retired";

        public int Interval => 1000000;
        public string Name => "InstructionRetired";

        public string Unit => "count";
    }
}