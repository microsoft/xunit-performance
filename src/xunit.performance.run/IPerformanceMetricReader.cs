// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Sdk
{
    public interface IPerformanceMetricReader
    {
        IEnumerable<PerformanceMetricInfo> GetMetrics(string testCase);
        List<Dictionary<string, double>> GetValues(string testCase);
    }
}
