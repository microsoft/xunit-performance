using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Api
{
    internal static partial class XunitBenchmark
    {
        internal sealed class PerformanceMetricInfoComparer : IEqualityComparer<PerformanceMetricInfo>
        {
            public bool Equals(PerformanceMetricInfo x, PerformanceMetricInfo y)
            {
                return x.GetType().Equals(y.GetType());
            }

            public int GetHashCode(PerformanceMetricInfo obj)
            {
                return obj.GetType().GetHashCode();
            }
        }
    }
}
