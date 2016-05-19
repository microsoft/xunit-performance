// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
#if !LINUX_BUILD
using MathNet.Numerics;
using MathNet.Numerics.Statistics;
#endif

namespace Microsoft.Xunit.Performance.Analysis
{
#if !LINUX_BUILD

    public static class MathExtensions
    {
        /// <summary>
        /// Calculates a confidence interval as a percentage of the mean
        /// </summary>
        /// <remarks>
        /// This assumes a roughly normal distribution in the sample data.
        /// </remarks>
        /// <param name="stats">A <see cref="RunningStatistics"/> object pre-populated with the sample data.</param>
        /// <param name="confidence">The desired confidence in the resulting interval.</param>
        /// <returns>The confidence interval, as a percentage of the mean.</returns>
        public static double MarginOfError(this RunningStatistics stats, double confidence)
        {
            if (stats.Count < 2)
                return double.NaN;

            var stderr = stats.StandardDeviation / Math.Sqrt(stats.Count);
            var t = TInv(1.0 - confidence, (int)stats.Count - 1);
            var mean = stats.Mean;
            var interval = t * stderr;

            return interval / mean;
        }

        [ThreadStatic]
        private static Dictionary<double, Dictionary<int, double>> t_TInvCache = new Dictionary<double, Dictionary<int, double>>();

        private static double TInv(double probability, int degreesOfFreedom)
        {
            Dictionary<int, double> dofCache;
            if (!t_TInvCache.TryGetValue(probability, out dofCache))
                t_TInvCache[probability] = dofCache = new Dictionary<int, double>();
            double result;
            if (!dofCache.TryGetValue(degreesOfFreedom, out result))
                dofCache[degreesOfFreedom] = result = ExcelFunctions.TInv(probability, degreesOfFreedom);
            return result;
        }
    }

#else

    //
    // Just mock up the type for build to complete on Linux 
    //
    public static class MathExtensions
    {
        public static double MarginOfError(this RunningStatistics stats, double confidence)
        {
            return 0;
        }    
    }

#endif
}

