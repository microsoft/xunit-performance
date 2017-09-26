namespace Microsoft.Xunit.Performance.Api
{
    public sealed class PerformanceMonitorCounter
    {
        public PerformanceMonitorCounter(string displayName, string name, string unit, int id)
        {
            DisplayName = displayName;
            Name = name;
            Unit = unit;
            Id = id;
        }

        public string DisplayName { get; }

        public string Name { get; }

        public string Unit { get; }

        public int Id { get; }
    }
}
