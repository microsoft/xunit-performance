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
    }
}
