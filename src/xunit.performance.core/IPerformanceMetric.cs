namespace Microsoft.Xunit.Performance
{
    /// <summary>
    /// Implemented by types which provide metrics for performance tests.
    /// </summary>
    public interface IPerformanceMetric
    {
        string DisplayName { get; }
    }
}
