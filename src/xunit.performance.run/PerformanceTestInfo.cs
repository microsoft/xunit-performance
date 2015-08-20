using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance
{
    class PerformanceTestInfo
    {
        public XunitProjectAssembly Assembly;
        public ITestCase TestCase;
        public IEnumerable<PerformanceMetric> Metrics;
    }
}
