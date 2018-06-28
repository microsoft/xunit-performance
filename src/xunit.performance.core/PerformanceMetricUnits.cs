// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xunit.Performance.Sdk
{
    /// <summary>
    /// Provides standard strings for <see cref="PerformanceMetricInfo.Unit"/>
    /// </summary>
    public static class PerformanceMetricUnits
    {
        /// <summary>
        /// Indicates that a performance metric's value is measured in bytes.
        /// </summary>
        public const string Bytes = "bytes";

        /// <summary>
        /// Indicates that a performance metric's value is a unitless count.
        /// </summary>
        public const string Count = "count";

        /// <summary>
        /// Indicates that a performance metric's value is measures in milliseconds.
        /// </summary>
        public const string Milliseconds = "msec";
    }
}