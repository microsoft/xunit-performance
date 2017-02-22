using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance.Api
{
    internal sealed class PerformanceTestMessage
    {
        public ITestCase TestCase { get; set; }

        public IEnumerable<PerformanceMetricInfo> Metrics { get; set; }
    }
}
