// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Api
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
        /// Unique identifier for the associated process that loaded this module.
        /// </summary>
        public int ProcessId { get; set; }

        /// <summary>
        /// The size of the module when loaded in memory.
        /// </summary>
        public int ImageSize { get; set; }

        /// <summary>
        /// The address where the module was loaded.
        /// </summary>
        public ulong StartAddress { get; set; }

        /// <summary>
        /// StartAddress + ImageSize
        /// </summary>
        public ulong EndAddress => StartAddress + (uint)ImageSize;

        internal int Checksum { get; set; }

        /// <summary>
        /// FIXME: Should PerformanceMonitorCounterData be exposed via a IReadOnlyDictionary?
        /// </summary>
        public IDictionary<int, long> PerformanceMonitorCounterData { get; set; }

        /// <summary>
        /// Indicates whether the modules was loaded.
        /// </summary>
        internal bool IsLoaded { get; set; } = false;
    }
}
