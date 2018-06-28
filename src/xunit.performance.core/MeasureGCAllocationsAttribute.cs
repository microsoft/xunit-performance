// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance.Sdk;
using System;

namespace Microsoft.Xunit.Performance
{
    /// <summary>
    /// An attribute that is applied to a method, class, or assembly, to indicate that the performance test framework
    /// should collect and report the total size of objects allocated on the GC heap.
    /// </summary>
    /// <remarks>
    /// Note that the underlying GC events used to collect this data report samples of aggregates of object allocations.
    /// Even if every iteration of the test allocates, many iterations may report zero allocations.  The average value collected
    /// across all iterations should give a meaningful measure of per-iteration allocations.
    /// </remarks>
    [PerformanceMetricDiscoverer("Microsoft.Xunit.Performance.GCAllocationsMetricDiscoverer", "xunit.performance.metrics")]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
    public sealed class MeasureGCAllocationsAttribute : Attribute, IPerformanceMetricAttribute
    {
    }
}