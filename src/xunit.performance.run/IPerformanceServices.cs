// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance.Sdk;
using System;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance
{
    public interface IPerformanceServices
    {
        IDisposable StartTracing(IEnumerable<PerformanceTestInfo> tests, string pathBase);
        IPerformanceMetricReader GetPerformanceMetricReader(IEnumerable<PerformanceTestInfo> tests, string etlPath, string runId);
        string RuntimeVersion { get; }
    }
}
