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
        /// <summary>
        /// Initializes a new instance of the EtwModule class.
        /// </summary>
        /// <param name="fullName"></param>
        /// <param name="checksum"></param>
        public EtwModule(string fullName, int checksum)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentNullException(nameof(fullName));

            FullName = fullName;
            Checksum = checksum;
            LifeSpan = new EtwLifeSpan();

            PerformanceMonitorCounterData = new Dictionary<int, long>();
        }

        /// <summary>
        /// The fully qualified name of the module file.
        /// </summary>
        public string FullName { get; }

        /// <summary>
        /// Module's checksum.
        /// </summary>
        internal int Checksum { get; }

        /// <summary>
        /// TODO: Should PerformanceMonitorCounterData be exposed via a IReadOnlyDictionary?
        /// </summary>
        public IDictionary<int, long> PerformanceMonitorCounterData { get; private set; }

        /// <summary>
        /// Represents the address space where this module was loaded.
        /// </summary>
        internal EtwAddressSpace AddressSpace { get; set; }

        /// <summary>
        /// Life span of this module (From the time it was loaded until the time it was unloaded).
        /// </summary>
        internal EtwLifeSpan LifeSpan { get; }

        internal EtwModule Copy()
        {
            var newModule = new EtwModule(FullName, Checksum) {
                AddressSpace = AddressSpace,
                PerformanceMonitorCounterData = new Dictionary<int, long>(PerformanceMonitorCounterData),
            };

            newModule.LifeSpan.Start = LifeSpan.Start;
            newModule.LifeSpan.End = LifeSpan.End;

            return newModule;
        }
    }
}
