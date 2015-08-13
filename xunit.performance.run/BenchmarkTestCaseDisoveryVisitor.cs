using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance
{
    internal class BenchmarkTestCaseDiscoveryVisitor : TestMessageVisitor<IDiscoveryCompleteMessage>
    {
        public readonly List<BenchmarkTestInfo> Tests = new List<BenchmarkTestInfo>();
        private XunitProjectAssembly _assembly;
        private XunitFilters _filters;

        public BenchmarkTestCaseDiscoveryVisitor(XunitProjectAssembly assembly, XunitFilters filters)
        {
            _assembly = assembly;
            _filters = filters;
        }

        protected override bool Visit(ITestCaseDiscoveryMessage testCaseDiscovered)
        {
            var testCase = testCaseDiscovered.TestCase;

            if (testCase.Traits.GetOrDefault("Benchmark")?.Contains("true") ?? false &&
                string.IsNullOrEmpty(testCase.SkipReason) &&
                _filters.Filter(testCase))
            {
                Tests.Add(new BenchmarkTestInfo { Assembly = _assembly, TestCase = testCaseDiscovered.TestCase });
            }

            return true;
        }
    }
}
