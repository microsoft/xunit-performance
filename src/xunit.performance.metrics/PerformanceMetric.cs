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
    public abstract class PerformanceMetric
    {
        public PerformanceMetric(string name, string unit, PerformanceMetricInterpretation interpretation)
        {
            Name = name;
            Unit = unit;
            Interpretation = interpretation;
        }

        public string Name { get; }
        public string Unit { get; }
        public PerformanceMetricInterpretation Interpretation { get; }

        public virtual IEnumerable<ProviderInfo> ProviderInfo => Enumerable.Empty<ProviderInfo>();

        public virtual PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context) => null;
    }
}
