// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Xunit.Performance.Sdk
{
    /// <summary>
    /// Base type for types which provide metrics for performance tests.
    /// </summary>
    public abstract class PerformanceMetric : PerformanceMetricInfo
    {
        public PerformanceMetric(string id, string displayName, string unit)
        {
            Id = id;
            DisplayName = displayName;
            Unit = unit;
        }

        public virtual IEnumerable<ProviderInfo> ProviderInfo => Enumerable.Empty<ProviderInfo>();

        public virtual PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context) => null;
    }
}
