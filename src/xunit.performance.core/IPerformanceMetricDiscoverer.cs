// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance.Sdk
{
    /// <summary>
    /// Implemented by discoverers that provide metrics to performance tests.
    /// </summary>
    public interface IPerformanceMetricDiscoverer
    {
        /// <summary>
        /// Gets the performance metrics from the metric attribute.
        /// </summary>
        /// <param name="metricAttribute"></param>
        /// <returns></returns>
        IEnumerable<PerformanceMetricInfo> GetMetrics(IAttributeInfo metricAttribute);
    }
}