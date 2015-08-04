using System;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.Xunit.Performance
{
    [XunitTestCaseDiscoverer("Microsoft.Xunit.Performance.BenchmarkDiscoverer", "xunit.performance")]
    [TraitDiscoverer("Microsoft.Xunit.Performance.BenchmarkTraitDiscoverer", "xunit.performance")]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class BenchmarkAttribute : FactAttribute, ITraitAttribute
    {
        /// <summary>
        /// The desired margin of error in the test results.  The default is 0.05, meaning that we want to measure the
        /// method's execution time to within +/-5%.  A smaller value allows detection of smaller changes in performance, but
        /// requires more test iterations, and thus more overall test execution time.  A larger value yields less accurate
        /// results, but in a shorter time.  
        /// </summary>
        public double MarginOfError { get; set; }
        internal const double DefaultMarginOfError = 0.10;

        /// <summary>
        /// The desired confidence in the test results.  The default is 0.95, meaning that we want to be 95% confident that
        /// the method's average execution time is within the range specified by <see cref="MarginOfError"/>.  Higher confidence
        /// values result in more test iterations, and thus more overall test execution time.
        /// </summary>
        public double Confidence { get; set; }
        internal const double DefaultConfidence = 0.95;

        /// <summary>
        /// If set, indicates whether the test allocates from the GC heap.  If true, test iterations will run until a GC occurs (and may
        /// continue to run due to other factors).  If null, the test framework will attempt to determine whether the test has been
        /// allocating.
        /// </summary>
        public bool? TriggersGC { get; set; }

        /// <summary>
        /// If true, performance metrics will be computed for all iterations of the method.  If false (the default), the first
        /// iteration of the method will be ignored when computing results.  This "warmup" iteration helps eliminate one-time costs 
        /// (such as JIT compilation time) from the results, but may be unnecessary for some tests.
        /// </summary>
        public bool SkipWarmup { get; set; }
    }
}
