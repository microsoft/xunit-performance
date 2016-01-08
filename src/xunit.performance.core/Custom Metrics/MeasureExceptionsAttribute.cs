using Microsoft.Xunit.Performance.Sdk;
using System;

namespace Microsoft.Xunit.Performance
{
    /// <summary>
    /// An attribute that is applied to a method, class, or assembly, to indicate that the performance test framework
    /// should collect and report the total number of Exception/Start events
    /// </summary>
    [PerformanceMetricDiscoverer("Microsoft.Xunit.Performance.ExceptionsMetricDiscoverer", "xunit.performance.metrics")]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
    public class MeasureExceptionsAttribute : Attribute, IPerformanceMetricAttribute
    {
    }
}
