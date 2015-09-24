// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance
{
    public class PerformanceTestInfo
    {
        public XunitProjectAssembly Assembly;
        public ITestCase TestCase;
        public IEnumerable<PerformanceMetricInfo> Metrics;
    }
}
