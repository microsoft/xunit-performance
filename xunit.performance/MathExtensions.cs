using MathNet.Numerics.Statistics;
using System;

namespace Microsoft.Xunit.Performance.Analysis
{
    public static class MathExtensions
    {
        public static double MarginOfError(this RunningStatistics stats, double confidence)
        {
            if (stats.Count < 2)
                return double.NaN;

            var stderr = stats.StandardDeviation / Math.Sqrt(stats.Count);
            var t = MathNet.Numerics.ExcelFunctions.TInv(1.0 - confidence, (int)stats.Count - 1);
            var mean = stats.Mean;
            var interval = t * stderr;

            return interval / mean;
        }
    }
}
