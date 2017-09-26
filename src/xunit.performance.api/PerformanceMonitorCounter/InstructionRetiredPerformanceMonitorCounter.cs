namespace Microsoft.Xunit.Performance.Api
{
    internal sealed class InstructionRetiredPerformanceMonitorCounter : IPerformanceMonitorCounter
    {
        public string DisplayName => "Instructions Retired";

        public string Name => "InstructionRetired";

        public string Unit => "count";

        public int Interval => 1000000;
    }
}
