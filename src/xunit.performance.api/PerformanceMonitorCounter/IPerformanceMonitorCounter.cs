namespace Microsoft.Xunit.Performance.Api
{
    interface IPerformanceMonitorCounter
    {
        string DisplayName { get; }

        string Name { get; }

        string Unit { get; }

        int Interval { get; }
    }
}
