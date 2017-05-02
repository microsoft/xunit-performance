using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Api
{
    internal struct XUnitPerformanceMetricData
    {
        public IEnumerable<ProviderInfo> Providers { get; set; }

        public IEnumerable<PerformanceTestMessage> PerformanceTestMessages { get; set; }
    }
}
