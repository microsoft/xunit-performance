// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Xunit.Performance.Sdk
{
    /// <summary>
    /// Provides information about a performance metric
    /// </summary>
    public abstract class PerformanceMetricInfo
    {
        /// <summary>
        /// Gets a string that uniquely identifies this metric, and can be used as an element or attribute name in XML output.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the human-readable name of the metric.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets a string describing the units of measurement for this metric.  Use one of the pre-defined strings in <see cref="PerformanceMetricUnits"/>.
        /// </summary>
        public string Unit { get; }

        /// <summary>
        /// Constructs a new PerformamceMetricInfo
        /// </summary>
        /// <param name="id"></param>
        /// <param name="displayName"></param>
        /// <param name="unit"></param>
        public PerformanceMetricInfo(string id, string displayName, string unit)
        {
            Id = id;
            DisplayName = displayName;
            Unit = unit;
        }
    }
}
