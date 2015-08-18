using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Xunit.Performance.Sdk
{
    /// <summary>
    /// Base type for types which provide metrics for performance tests.
    /// </summary>
    public abstract class PerformanceMetric
    {
        public PerformanceMetric(string displayName) { DisplayName = displayName; }

        public string DisplayName { get; private set; }

        public virtual IEnumerable<ProviderInfo> ProviderInfo => Enumerable.Empty<ProviderInfo>();
    }
}
