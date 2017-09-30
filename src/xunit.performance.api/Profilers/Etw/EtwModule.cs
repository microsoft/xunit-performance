// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.Xunit.Performance.Api.Profilers.Etw
{
    /// <summary>
    /// Loaded module for the corresponding process.
    /// </summary>
    public sealed class EtwModule
    {
        /// <summary>
        /// The fully qualified name of the module file.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// File name and extension of the module.
        /// </summary>
        public string ImageName => Path.GetFileName(FileName);

        /// <summary>
        /// TODO: Should PerformanceMonitorCounterData be exposed via a IReadOnlyDictionary?
        /// </summary>
        public IDictionary<int, long> PerformanceMonitorCounterData { get; set; }

        /// <summary>
        /// Indicates whether the modules was loaded.
        /// </summary>
        internal bool IsLoaded { get; set; } = false;

        /// <summary>
        /// Represents the address range where this module was loaded.
        /// </summary>
        internal EtwAddressRange AddressRange { get; set; }

        /// <summary>
        /// 
        /// </summary>
        internal int Checksum { get; set; }
    }
}
