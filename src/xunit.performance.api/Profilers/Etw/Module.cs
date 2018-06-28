// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Api.Profilers.Etw
{
    /// <summary>
    /// Loaded module for the corresponding process.
    /// </summary>
    public class Module
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Module"/> class.
        /// </summary>
        /// <param name="fullName"></param>
        /// <param name="checksum"></param>
        /// <param name="monitoredCounters">A set of monitored <see cref="PerformanceMonitorCounter"/> counters.</param>
        public Module(string fullName, int checksum, ISet<PerformanceMonitorCounter> monitoredCounters)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentNullException(nameof(fullName));

            FullName = fullName;
            Checksum = checksum;

            PerformanceMonitorCounterData = new Dictionary<PerformanceMonitorCounter, long>();
            foreach (var pmc in monitoredCounters)
                PerformanceMonitorCounterData.Add(pmc, 0);

            RuntimeInstances = new List<RuntimeInstance>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Module"/> class (Copy constructor).
        /// </summary>
        /// <param name="src">Instance to be copied.</param>
        public Module(Module src)
        {
            FullName = src.FullName;
            Checksum = src.Checksum;

            PerformanceMonitorCounterData = new Dictionary<PerformanceMonitorCounter, long>(src.PerformanceMonitorCounterData);
            RuntimeInstances = new List<RuntimeInstance>(src.RuntimeInstances);
        }

        /// <summary>
        /// Module's checksum.
        /// </summary>
        public int Checksum { get; }

        /// <summary>
        /// The fully qualified name of the module file.
        /// </summary>
        public string FullName { get; }

        /// <summary>
        /// TODO: Should PerformanceMonitorCounterData be exposed via a IReadOnlyDictionary?
        /// </summary>
        public IDictionary<PerformanceMonitorCounter, long> PerformanceMonitorCounterData { get; set; }

        /// <summary>
        /// A collection of runtime information (lifetime and loaded address) about this module.
        /// </summary>
        internal IList<RuntimeInstance> RuntimeInstances { get; }
    }
}