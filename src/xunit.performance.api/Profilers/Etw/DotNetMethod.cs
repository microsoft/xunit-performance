// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xunit.Performance.Api.Profilers.Etw
{
    /// <summary>
    /// Specifies a set of values that represent a loaded .NET method.
    /// </summary>
    internal sealed class DotNetMethod
    {
        /// <summary>
        /// Method's Id.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Method's name.
        /// </summary>
        public string MethodName { get; set; }

        /// <summary>
        /// Method's namespace.
        /// </summary>
        public string MethodNamespace { get; set; }

        /// <summary>
        /// Gets a value indicating whether the associated method is dynamic.
        /// </summary>
        internal bool IsDynamic { get; set; }

        /// <summary>
        /// Gets a value indicating whether the associated method is generic.
        /// </summary>
        internal bool IsGeneric { get; set; }

        /// <summary>
        /// Gets a value indicating whether the associated method has been jitted.
        /// </summary>
        internal bool IsJitted { get; set; }

        /// <summary>
        /// Life span of this method (From the time it was loaded until the time it was unloaded).
        /// </summary>
        internal LifeSpan LifeSpan { get; } = new LifeSpan();

        /// <summary>
        /// Represents the address space where this method was loaded.
        /// </summary>
        internal AddressSpace AddressSpace { get; set; }
    }
}
