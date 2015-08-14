using System;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.Xunit.Performance
{
    [XunitTestCaseDiscoverer("Microsoft.Xunit.Performance.BenchmarkDiscoverer", "xunit.performance.execution.{Platform}")]
    [TraitDiscoverer("Microsoft.Xunit.Performance.BenchmarkTraitDiscoverer", "xunit.performance.execution.{Platform}")]
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
