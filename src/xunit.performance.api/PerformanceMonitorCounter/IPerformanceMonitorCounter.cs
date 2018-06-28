namespace Microsoft.Xunit.Performance.Api
{
    interface IPerformanceMonitorCounter
    {
        string DisplayName { get; }

        int Interval { get; }
        string Name { get; }

        string Unit { get; }
    }
}