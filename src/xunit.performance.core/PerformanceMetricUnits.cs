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
    /// Provides standard strings for <see cref="PerformanceMetricInfo.Unit"/>
    /// </summary>
    public static class PerformanceMetricUnits
    {
        /// <summary>
        /// Indicates that a performance metric's value is a unitless count.
        /// </summary>
        public const string Count = "count";

        /// <summary>
        /// Indicates that a performance metric's value is measures in milliseconds.
        /// </summary>
        public const string Milliseconds = "msec";

        /// <summary>
        /// Indicates that a performance metric's value is measured in megabytes (1,048,576 bytes).
        /// </summary>
        public const string Megabytes = "mbyte";
    }
}
