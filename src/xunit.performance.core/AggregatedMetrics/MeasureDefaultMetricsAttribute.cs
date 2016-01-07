using Microsoft.Xunit.Performance.Sdk;
using System;

namespace Microsoft.Xunit.Performance
{
    /// <summary>
    /// An attribute that is applied to a method, class, or assembly, to indicate that the performance test framework
    /// should collect and report the total number of app domain destructions
    /// </summary>
    [PerformanceMetricDiscoverer("Microsoft.Xunit.Performance.AppDomainLoadMetricDiscoverer", "xunit.performance.metrics")]
    [PerformanceMetricDiscoverer("Microsoft.Xunit.Performance.AssemblyLoadMetricDiscoverer", "xunit.performance.metrics")]
    [PerformanceMetricDiscoverer("Microsoft.Xunit.Performance.DllsLoadedMetricDiscoverer", "xunit.performance.metrics")]
    [PerformanceMetricDiscoverer("Microsoft.Xunit.Performance.ExceptionsMetricDiscoverer", "xunit.performance.metrics")]
    [PerformanceMetricDiscoverer("Microsoft.Xunit.Performance.FileIOReadMetricDiscoverer", "xunit.performance.metrics")]
    [PerformanceMetricDiscoverer("Microsoft.Xunit.Performance.FileIOWriteMetricDiscoverer", "xunit.performance.metrics")]
    [PerformanceMetricDiscoverer("Microsoft.Xunit.Performance.FilesReadMetricDiscoverer", "xunit.performance.metrics")]
    [PerformanceMetricDiscoverer("Microsoft.Xunit.Performance.GCCountMetricDiscoverer", "xunit.performance.metrics")]
    [PerformanceMetricDiscoverer("Microsoft.Xunit.Performance.ModuleLoadMetricDiscoverer", "xunit.performance.metrics")]
    [PerformanceMetricDiscoverer("Microsoft.Xunit.Performance.ObjectsAllocatedMetricDiscoverer", "xunit.performance.metrics")]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
    public class MeasureDefaultMetricsAttribute : Attribute, IPerformanceMetricAttribute
    {
    }
}
