// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit.Sdk;

namespace Microsoft.Xunit.Performance
{
    /// <summary>
    /// Apply this attribute to an assembly if it contains benchmarks.  This attribute configures xUnit for benchmark execution.
    /// </summary>
    [TestFrameworkDiscoverer("Microsoft.Xunit.Performance.BenchmarkTestFrameworkTypeDiscoverer", "xunit.performance.execution")]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class OptimizeForBenchmarksAttribute : Attribute, ITestFrameworkAttribute
    {
    }
}