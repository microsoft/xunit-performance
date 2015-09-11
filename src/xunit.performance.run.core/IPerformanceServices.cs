// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Xunit.Performance
{
    public interface IPerformanceServices
    {
        IDisposable StartTracing(IEnumerable<PerformanceTestInfo> tests, string pathBase);
        PerformanceMetricEvaluationContextImpl GetPerformanceMetricEvaluationContext(IEnumerable<PerformanceTestInfo> tests, string etlPath, string runId);
        string RuntimeVersion { get; }
    }
}
