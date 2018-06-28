// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing.Session;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.Xunit.Performance.Api.Common;

namespace Microsoft.Xunit.Performance.Sdk
{
    /// <summary>
    /// Base type for types which provide metrics for performance tests.
    /// </summary>
    public abstract class PerformanceMetric : PerformanceMetricInfo
    {
        protected PerformanceMetric(string id, string displayName, string unit)
            : base(id, displayName, unit)
        {
        }

        public virtual IEnumerable<ProviderInfo> ProviderInfo => Enumerable.Empty<ProviderInfo>();

        public static int GetProfileSourceInfoId(string key)
        {
            if (IsWindowsPlatform && TraceEventProfileSources.GetInfo().TryGetValue(key, out ProfileSourceInfo profileSourceInfo))
                return profileSourceInfo.ID;
            else
                return -1;
        }

        public virtual PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context) => null;
    }
}