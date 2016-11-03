// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.Xunit.Performance.Sdk
{
    /// <summary>
    /// Provides standard strings for <see cref="PerformanceMetricInfo.BetterDirection"/>
    /// </summary>
    public class PerformanceMetricBetterDirections
    {
        /// <summary>
        /// Indicates that a performance measurement of a given metric is better if larger
        /// (e.g.: framerate, flops, score, etc.)
        /// </summary>
        public const string Ascending = "asc";

        /// <summary>
        /// Indicates that a performance measurement of a given metric is better if smaller
        /// (e.g.: duration, memory consumption, disk utilization, etc.)
        /// </summary>
        public const string Descending = "desc";
    }
}
