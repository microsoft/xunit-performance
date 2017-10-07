// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Api.Profilers.Etw
{
    /// <summary>
    /// Loaded module for the corresponding process.
    /// </summary>
    public class EtwModule
    {
        public EtwModule(string fullName, int checksum)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentNullException(nameof(fullName));

            FullName = fullName;
            Checksum = checksum;
            PerformanceMonitorCounterData = new Dictionary<int, long>();

            LoadTimeStamp = DateTime.MinValue;
            UnloadTimeStamp = DateTime.MaxValue;
        }

        /// <summary>
        /// The fully qualified name of the module file.
        /// </summary>
        public string FullName { get; }

        /// <summary>
        /// TODO: Should PerformanceMonitorCounterData be exposed via a IReadOnlyDictionary?
        /// </summary>
        public IDictionary<int, long> PerformanceMonitorCounterData { get; }

        /// <summary>
        /// Indicates whether the modules was loaded.
        /// </summary>
        internal bool IsLoaded { get; set; } = false;

        /// <summary>
        /// Represents the address range where this module was loaded.
        /// </summary>
        internal EtwAddressRange AddressRange { get; set; }

        /// <summary>
        /// Timestamp when the module was loaded.
        /// </summary>
        internal DateTime LoadTimeStamp { get; set; }

        /// <summary>
        /// Timestamp when the module was unloaded.
        /// </summary>
        internal DateTime UnloadTimeStamp { get; set; }

        /// <summary>
        /// Module's checksum.
        /// </summary>
        internal int Checksum { get; }
    }
}
