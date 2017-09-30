// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Api.Profilers.Etw
{
    /// <summary>
    /// Provides information about a profiled process.
    /// </summary>
    public sealed class EtwProcess
    {
        /// <summary>
        /// Name of the process.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Unique identifier associated with this process.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The time that the associated process was started.
        /// </summary>
        /// <remarks>
        /// It will be null if the process started before the ETW session was enabled.
        /// </remarks>
        public DateTime? Start { get; set; }

        /// <summary>
        /// The time that the associated process exited.
        /// </summary>
        /// <remarks>
        /// It will be null if the process continued running when the ETW session was stopped.
        /// </remarks>
        public DateTime? Exit { get; set; }

        /// <summary>
        /// TODO: Should PerformanceMonitorCounterData be exposed via a IReadOnlyDictionary?
        /// </summary>
        public IDictionary<int, long> PerformanceMonitorCounterData { get; set; }

        /// <summary>
        /// The modules that have been loaded by the associated process.
        /// </summary>
        public IList<EtwModule> Modules { get; set; }

        /// <summary>
        /// The managed .NET modules that have been loaded by the associated process.
        /// </summary>
        public IList<EtwManagedModule> ManagedModules { get; set; }

        /// <summary>
        /// Unique identifier associated with this process' parent.
        /// </summary>
        internal int? ParentId { get; set; }
    }
}
