// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Api.Profilers.Etw
{
    /// <summary>
    /// Specifies a set of values that represent a loaded .NET method.
    /// </summary>
    sealed class DotNetMethod
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DotNetMethod"/> class.
        /// </summary>
        /// <param name="id">Method id</param>
        /// <param name="name">Method name</param>
        /// <param name="namespace">Method namespace</param>
        /// <param name="isDynamic">Flag indicating whether the method is dynamic.</param>
        /// <param name="isGeneric">Flag indicating whether the method is generic.</param>
        /// <param name="isJitted">Flag indicating whether the method is jitted.</param>
        public DotNetMethod(long id, string name, string @namespace, bool isDynamic, bool isGeneric, bool isJitted)
        {
            Id = id;
            Name = name;
            Namespace = @namespace;
            IsDynamic = isDynamic;
            IsGeneric = isGeneric;
            IsJitted = isJitted;

            RuntimeInstances = new List<RuntimeInstance>();
        }

        /// <summary>
        /// Method's Id.
        /// </summary>
        public long Id { get; }

        /// <summary>
        /// Method's name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Method's namespace.
        /// </summary>
        public string Namespace { get; }

        /// <summary>
        /// Gets a value indicating whether the associated method is dynamic.
        /// </summary>
        internal bool IsDynamic { get; }

        /// <summary>
        /// Gets a value indicating whether the associated method is generic.
        /// </summary>
        internal bool IsGeneric { get; }

        /// <summary>
        /// Gets a value indicating whether the associated method has been jitted.
        /// </summary>
        internal bool IsJitted { get; }

        /// <summary>
        /// A collection of runtime information (lifetime and loaded address) about this module.
        /// </summary>
        internal IList<RuntimeInstance> RuntimeInstances { get; }
    }
}