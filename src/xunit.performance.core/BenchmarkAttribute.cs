using System;
using Xunit;
using Xunit.Sdk;
using Microsoft.Xunit.Performance.Sdk;

namespace Microsoft.Xunit.Performance
{
    /// <summary>
    /// Attribute that is applied to a method to indicate that it is a performance test that
    /// should be run and measured by the performance test runner.
    /// </summary>
    [XunitTestCaseDiscoverer("Microsoft.Xunit.Performance.BenchmarkDiscoverer", "xunit.performance.execution.{Platform}")]
    [PerformanceMetricDiscoverer("Microsoft.Xunit.Performance.BenchmarkMetricDiscoverer", "xunit.performance.metrics")]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class BenchmarkAttribute : FactAttribute, ITraitAttribute
    {
        /// <summary>
        /// If true, performance metrics will be computed for all iterations of the method.  If false (the default), the first
        /// iteration of the method will be ignored when computing results.  This "warmup" iteration helps eliminate one-time costs 
        /// (such as JIT compilation time) from the results, but may be unnecessary for some tests.
        /// </summary>
        public bool SkipWarmup { get; set; }
    }
}
