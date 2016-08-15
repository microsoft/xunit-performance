// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Analysis
{
    internal class Properties
    {
        public const double ErrorConfidence = 0.95; // TODO: make configurable

        public static Dictionary<string, MetricInfo> AllMetrics = new Dictionary<string, MetricInfo>();

        /// <summary>
        /// The name of the Duration metric, as provided by the XML.
        /// </summary>
        public const string DurationMetricName = "Duration";
        public const string GCAllocMetricName = "GCAlloc";
        public const string GCCountMetricName = "GCCount";
        public const string InstRetiredMetricName = "InstRetired";

    }
}
