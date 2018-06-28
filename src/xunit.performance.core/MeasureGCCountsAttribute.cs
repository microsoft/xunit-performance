// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance.Sdk;
using System;

namespace Microsoft.Xunit.Performance
{
    /// <summary>
    /// An attribute that is applied to a method, class, or assembly, to indicate that the performance test framework
    /// should collect and report GC counts for the given test(s).
    /// </summary>
    [PerformanceMetricDiscoverer("Microsoft.Xunit.Performance.GCCountMetricDiscoverer", "xunit.performance.metrics")]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
    public sealed class MeasureGCCountsAttribute : Attribute, IPerformanceMetricAttribute
    {
    }
}