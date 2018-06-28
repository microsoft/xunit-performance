using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Api
{
    static partial class XunitBenchmark
    {
        internal sealed class PerformanceMetricInfoComparer : IEqualityComparer<PerformanceMetricInfo>
        {
            public bool Equals(PerformanceMetricInfo x, PerformanceMetricInfo y) => x.GetType().Equals(y.GetType());

            public int GetHashCode(PerformanceMetricInfo obj) => obj.GetType().GetHashCode();
        }
    }
}