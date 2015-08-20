using Microsoft.Xunit.Performance.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Xunit.Performance
{
    /// <summary>
    /// An attribute that is applied to a method, class, or assembly, to indicate that the performance test framework
    /// should collect and report GC counts for the given test(s).
    /// </summary>
    [PerformanceMetricDiscoverer("Microsoft.Xunit.Performance.GCCountMetricDiscoverer", "xunit.performance.metrics")]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
    public class MeasureGCCountsAttribute : Attribute, IPerformanceMetricAttribute
    {
    }
}
