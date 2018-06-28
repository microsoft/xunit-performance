// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance.Sdk;
using System;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.Xunit.Performance
{
    /// <summary>
    /// Attribute that is applied to a method to indicate that it is a performance test that
    /// should be run and measured by the performance test runner.
    /// </summary>
    [XunitTestCaseDiscoverer("Microsoft.Xunit.Performance.BenchmarkDiscoverer", "xunit.performance.execution")]
    [PerformanceMetricDiscoverer("Microsoft.Xunit.Performance.BenchmarkMetricDiscoverer", "xunit.performance.metrics")]
    [TraitDiscoverer("Microsoft.Xunit.Performance.BenchmarkDiscoverer", "xunit.performance.execution")]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class BenchmarkAttribute : FactAttribute, IPerformanceMetricAttribute, ITraitAttribute
    {
        /// <summary>
        /// Get or set the count of inner iterations to run inside of each harness iteration.
        /// </summary>
        public long InnerIterationCount { get; set; } = 1;
    }
}