// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Xunit.Performance.Sdk
{
    public interface IPerformanceMetricReader
    {
        IEnumerable<PerformanceMetric> GetMetrics(string testCase);
    }
}
