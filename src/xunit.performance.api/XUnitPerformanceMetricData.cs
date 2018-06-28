using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Api
{
    struct XUnitPerformanceMetricData
    {
        public IEnumerable<PerformanceTestMessage> PerformanceTestMessages { get; set; }
        public IEnumerable<ProviderInfo> Providers { get; set; }
    }
}