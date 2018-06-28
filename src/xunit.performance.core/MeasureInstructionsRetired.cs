// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance.Sdk;
using System;

namespace Microsoft.Xunit.Performance
{
    /// <summary>
    /// An attribute that is applied to a method, class, or assembly, to indicate that the performance test framework
    /// should collect and report the number of instructions retired per test iteration.
    /// </summary>
    [PerformanceMetricDiscoverer("Microsoft.Xunit.Performance.InstructionsRetiredMetricDiscoverer", "xunit.performance.metrics")]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
    public sealed class MeasureInstructionsRetiredAttribute : Attribute, IPerformanceMetricAttribute
    {
        /// <summary>
        /// Mesaure instructions retired using the default sampling interval
        /// </summary>
        public MeasureInstructionsRetiredAttribute()
        {
        }

        /// <summary>
        /// Measure instructions retired using the specified sampling interval
        /// </summary>
        /// <param name="maxSampleInterval">
        /// The maximum number of retired instructions to include in each sample.  If this it outside of the valid
        /// range for the machine being tested, the nearest valid value will be used instead.
        /// A suggested starting point for this value is 100,000.
        /// </param>
        public MeasureInstructionsRetiredAttribute(int maxSampleInterval)
        {
        }
    }
}