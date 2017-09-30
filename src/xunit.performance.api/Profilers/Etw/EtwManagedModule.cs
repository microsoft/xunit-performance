// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Api.Profilers.Etw
{
    public sealed class EtwManagedModule
    {
        public long Id { get; set; }

        public string ILPath { get; set; }

        public string ILFileName { get; set; }

        /// <summary>
        /// TODO: Should PerformanceMonitorCounterData be exposed via a IReadOnlyDictionary?
        /// </summary>
        public IDictionary<int, long> PerformanceMonitorCounterData { get; set; }

        /// <summary>
        /// Collection of loaded methods for this module.
        /// </summary>
        internal IList<EtwManagedMethod> Methods { get; set; }

        /// <summary>
        /// Represents the address range where this method was loaded.
        /// </summary>
        internal EtwAddressRange AddressRange { get; set; }

        internal bool IsLoaded { get; set; } = false;

        internal DateTime? LoadTimeStamp { get; set; }

        internal DateTime? UnloadTimeStamp { get; set; }
    }
}
