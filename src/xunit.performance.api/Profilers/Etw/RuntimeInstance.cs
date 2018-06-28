// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Xunit.Performance.Api.Profilers.Etw
{
    /// <summary>
    /// Runtime information about the corresponding loaded code.
    /// </summary>
    sealed class RuntimeInstance
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RuntimeInstance"/> class.
        /// </summary>
        /// <param name="addressSpace">The address space where this code was loaded.</param>
        /// <param name="loadDateTime">The time when this code was loaded.</param>
        public RuntimeInstance(AddressSpace addressSpace, DateTime loadDateTime)
        {
            AddressSpace = addressSpace;
            LifeSpan = new LifeSpan { Start = loadDateTime, };
        }

        /// <summary>
        /// Represents the address space where this code was loaded.
        /// </summary>
        internal AddressSpace AddressSpace { get; }

        /// <summary>
        /// Life span of this code in memory (From the time it was loaded until the time it was unloaded).
        /// </summary>
        internal LifeSpan LifeSpan { get; }
    }
}