// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Xunit.Performance.Api.Profilers.Etw
{
    internal sealed class EtwDotNetMethod
    {
        public long Id { get; set; }

        public string MethodName { get; set; }

        public string MethodNamespace { get; set; }

        internal bool IsDynamic { get; set; }

        internal bool IsGeneric { get; set; }

        internal bool IsJitted { get; set; }

        /// <summary>
        /// Life span of this method (From the time it was loaded until the time it was unloaded).
        /// </summary>
        internal EtwLifeSpan LifeSpan { get; } = new EtwLifeSpan();

        /// <summary>
        /// Represents the address space where this method was loaded.
        /// </summary>
        internal EtwAddressSpace AddressSpace { get; set; }
    }
}
