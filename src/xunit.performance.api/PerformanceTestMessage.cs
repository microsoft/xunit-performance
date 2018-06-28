using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance.Api
{
    sealed class PerformanceTestMessage
    {
        public IEnumerable<PerformanceMetricInfo> Metrics { get; set; }
        public ITestCase TestCase { get; set; }
    }
}