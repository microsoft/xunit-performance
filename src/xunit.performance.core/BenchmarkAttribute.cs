// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;
using Xunit.Sdk;
using Microsoft.Xunit.Performance.Sdk;

namespace Microsoft.Xunit.Performance
{
    /// <summary>
    /// Attribute that is applied to a method to indicate that it is a performance test that
    /// should be run and measured by the performance test runner.
    /// </summary>
    [XunitTestCaseDiscoverer("Microsoft.Xunit.Performance.BenchmarkDiscoverer", "xunit.performance.execution.{Platform}")]
    [PerformanceMetricDiscoverer("Microsoft.Xunit.Performance.BenchmarkMetricDiscoverer", "xunit.performance.metrics")]
    [TraitDiscoverer("Microsoft.Xunit.Performance.BenchmarkDiscoverer", "xunit.performance.execution.{Platform}")]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class BenchmarkAttribute : FactAttribute, IPerformanceMetricAttribute, ITraitAttribute
    {
    }
}
