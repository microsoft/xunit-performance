// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Api.Profilers.Etw
{
    /// <summary>
    /// Provides information about a profiled process.
    /// </summary>
    public sealed class Process
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Process"/> class.
        /// </summary>
        /// <param name="name">Process image file name</param>
        /// <param name="id">Process Id</param>
        /// <param name="parentId">Process' parent Id</param>
        /// <param name="monitoredCounters">A set of monitored <see cref="PerformanceMonitorCounter"/> counters.</param>
        public Process(string name, int id, int parentId, ISet<PerformanceMonitorCounter> monitoredCounters)
        {
            Name = name;
            Id = id;
            ParentId = parentId;
            LifeSpan = new LifeSpan();

            PerformanceMonitorCounterData = new Dictionary<PerformanceMonitorCounter, long>();
            foreach (var pmc in monitoredCounters)
                PerformanceMonitorCounterData.Add(pmc, 0);

            Modules = new List<Module>();
        }

        /// <summary>
        /// Unique identifier associated with this process.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Life span of this process (From the time that the associated process was started until the time that the associated process exited).
        /// </summary>
        public LifeSpan LifeSpan { get; }

        /// <summary>
        /// The modules that have been loaded by the associated process.
        /// </summary>
        public IList<Module> Modules { get; }

        /// <summary>
        /// Name of the process.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Unique identifier associated with this process' parent.
        /// </summary>
        public int ParentId { get; }

        /// <summary>
        /// TODO: Should PerformanceMonitorCounterData be exposed via a IReadOnlyDictionary?
        /// </summary>
        public IDictionary<PerformanceMonitorCounter, long> PerformanceMonitorCounterData { get; }
    }
}