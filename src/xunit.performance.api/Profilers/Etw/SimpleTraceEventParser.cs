// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Xunit.Performance.Api.Profilers.Etw
{
    /// <summary>
    /// Provides a simple interface to extract ETW process/modules data from an *.etl file.
    /// </summary>
    public sealed class SimpleTraceEventParser
    {
        /// <summary>
        /// Gets profile data from the provided <see cref="ScenarioExecutionResult"/> object
        /// </summary>
        /// <param name="scenarioExecutionResult"></param>
        /// <returns>A collection of the profiled processes for the given scenario.</returns>
        /// <remarks>
        /// Some assumptions:
        ///     1. The scenario launches a single process, but itself can launch child processes.
        ///     2. Process started/stopped within the ETW session.
        /// </remarks>
        public IReadOnlyCollection<Process> GetProfileData(ScenarioExecutionResult scenarioExecutionResult)
        {
            var processes = new List<Process>();
            Module defaultNtoskrnlModule = null;
            var pmcSamplingIntervals = new Dictionary<int, long>();

            bool IsOurProcess(ProcessTraceData obj)
            {
                return obj.ProcessID == scenarioExecutionResult.ProcessExitInfo.ProcessId
                    || processes.Any(process => process.Id == obj.ProcessID || process.Id == obj.ParentID);
            }

            using (var source = new ETWTraceEventSource(scenarioExecutionResult.EventLogFileName))
            {
                if (source.EventsLost > 0)
                    throw new Exception($"Events lost in trace '{scenarioExecutionResult.EventLogFileName}'");

                const string UnknownModuleName = "Unknown";
                const int DefaultModuleChecksum = 0;

                ////////////////////////////////////////////////////////////////
                // Process data
                var parser = new KernelTraceEventParser(source);
                parser.ProcessStart += (ProcessTraceData obj) => {
                    if (IsOurProcess(obj))
                    {
                        var process = new Process(obj.ImageFileName, obj.ProcessID, obj.ParentID);
                        process.LifeSpan.Start = obj.TimeStamp;
                        processes.Add(process);
                    }
                };
                parser.ProcessStop += (ProcessTraceData obj) => {
                    if (IsOurProcess(obj))
                        processes.Single(process => process.Id == obj.ProcessID).LifeSpan.End = obj.TimeStamp;
                };

                ////////////////////////////////////////////////////////////////
                // Image/Module data
                parser.ImageLoad += (ImageLoadTraceData obj) => {
                    var process = processes.SingleOrDefault(p => p.Id == obj.ProcessID);
                    if (process == null)
                        return;

                    var module = new Module(obj.FileName, obj.ImageChecksum) {
                        AddressSpace = new AddressSpace(obj.ImageBase, (uint)obj.ImageSize)
                    };
                    module.LifeSpan.Start = obj.TimeStamp;

                    process.Modules.Add(module);
                };
                parser.ImageUnload += (ImageLoadTraceData obj) => {
                    var process = processes.SingleOrDefault(p => p.Id == obj.ProcessID);
                    if (process == null)
                        return;

                    // Check if the unloaded module is on the list.
                    // The module must be loaded, in the same address space, and same file name.
                    var module = process.Modules
                        .SingleOrDefault(m => m.LifeSpan.IsInInterval(obj.TimeStamp) == 0 && obj.Equivalent(m));
                    if (module == null)
                        return;
                    module.LifeSpan.End = obj.TimeStamp;
                };

                ////////////////////////////////////////////////////////////////
                // "ntoskrnl.exe" data
                parser.ImageDCStart += (ImageLoadTraceData obj) => {
                    if (obj.ProcessID == 0 && obj.FileName.EndsWith("ntoskrnl.exe"))
                    {
                        if (defaultNtoskrnlModule == null)
                        {
                            defaultNtoskrnlModule = new Module(obj.FileName, obj.ImageChecksum) {
                                AddressSpace = new AddressSpace(obj.ImageBase, (uint)obj.ImageSize)
                            };
                            defaultNtoskrnlModule.LifeSpan.Start = obj.TimeStamp;
                            defaultNtoskrnlModule.LifeSpan.End = DateTime.MaxValue;
                        }
                    }
                };

                ////////////////////////////////////////////////////////////////
                // PMC data
                var pmcSamples = new List<PmcSample>();
                parser.PerfInfoCollectionStart += (SampledProfileIntervalTraceData obj) => {
                    // Update the Pmc intervals.
                    if (scenarioExecutionResult.PerformanceMonitorCounters.Any(pmc => pmc.Id == obj.SampleSource))
                    {
                        if (!pmcSamplingIntervals.ContainsKey(obj.SampleSource))
                            pmcSamplingIntervals.Add(obj.SampleSource, obj.NewInterval);
                        else
                            pmcSamplingIntervals[obj.SampleSource] = obj.NewInterval;
                    }
                };
                parser.PerfInfoPMCSample += (PMCCounterProfTraceData obj) => {
                    var process = processes.SingleOrDefault(p => p.Id == obj.ProcessID);
                    if (process == null)
                        return;

                    var performanceMonitorCounter = scenarioExecutionResult.PerformanceMonitorCounters
                        .SingleOrDefault(pmc => pmc.Id == obj.ProfileSource);
                    if (performanceMonitorCounter == null)
                        return;

                    // If this is our process and it is a Pmc we care to measure.
                    if (!process.PerformanceMonitorCounterData.ContainsKey(performanceMonitorCounter))
                        process.PerformanceMonitorCounterData.Add(performanceMonitorCounter, 0);
                    process.PerformanceMonitorCounterData[performanceMonitorCounter] += pmcSamplingIntervals[obj.ProfileSource];

                    // Is the IP in the kernel (Under this process)?
                    if (defaultNtoskrnlModule != null && obj.IsInTimeAndAddressIntervals(defaultNtoskrnlModule))
                    {
                        var krnlModule = process.Modules
                            .SingleOrDefault(m => m.Checksum == defaultNtoskrnlModule.Checksum
                                && m.AddressSpace == defaultNtoskrnlModule.AddressSpace
                                && m.FullName == defaultNtoskrnlModule.FullName);
                        if (krnlModule == null)
                        {
                            krnlModule = defaultNtoskrnlModule.Copy();
                            process.Modules.Add(krnlModule);
                        }
                    }

                    var modules = process.Modules
                        .Where(m => {
                            return obj.IsInTimeAndAddressIntervals(m);
                        })
                        .Select(m => m);

                    if (modules.Count() == 0)
                    {
                        // This might fall in managed code. We need to buffer and test it afterwards.
                        pmcSamples.Add(new PmcSample {
                            InstructionPointer = obj.InstructionPointer,
                            ProcessId = obj.ProcessID,
                            ProfileSourceId = obj.ProfileSource,
                            TimeStamp = obj.TimeStamp,
                            SamplingInterval = pmcSamplingIntervals[obj.ProfileSource],
                        });
                        return;
                    }

                    modules.ForEach(module => {
                        if (!module.PerformanceMonitorCounterData.ContainsKey(performanceMonitorCounter))
                            module.PerformanceMonitorCounterData.Add(performanceMonitorCounter, 0);
                        module.PerformanceMonitorCounterData[performanceMonitorCounter] += pmcSamplingIntervals[obj.ProfileSource];
                    });
                };

                ////////////////////////////////////////////////////////////////
                // .NET modules
                parser.Source.Clr.LoaderModuleLoad += (ModuleLoadUnloadTraceData obj) => {
                    var process = processes.SingleOrDefault(p => p.Id == obj.ProcessID);
                    if (process == null)
                        return;

                    var modulePath = string.IsNullOrEmpty(obj.ModuleNativePath) ? obj.ModuleILPath : obj.ModuleNativePath;
                    var module = process.Modules
                        .SingleOrDefault(m => m.FullName == modulePath && m.LifeSpan.IsInInterval(obj.TimeStamp) == 0);
                    if (module == null)
                    {
                        // Not previously loaded (For example, 'Anonymously Hosted DynamicMethods Assembly')
                        module = new DotNetModule(modulePath, DefaultModuleChecksum, obj.ModuleID);
                        process.Modules.Add(module);
                    }
                    else
                    {
                        // Update/Swap the module. It is a .NET module.
                        var dotnetModule = new DotNetModule(module.FullName, module.Checksum, obj.ModuleID) {
                            AddressSpace = module.AddressSpace,
                        };
                        dotnetModule.LifeSpan.Start = obj.TimeStamp;

                        process.Modules.Remove(module);
                        process.Modules.Add(dotnetModule);
                    }
                };
                parser.Source.Clr.LoaderModuleUnload += (ModuleLoadUnloadTraceData obj) => {
                    var process = processes
                        .SingleOrDefault(p => p.Id == obj.ProcessID);
                    if (process == null)
                        return;

                    var module = process.Modules
                        .OfType<DotNetModule>()
                        .SingleOrDefault(m => m.Id == obj.ModuleID);
                    if (module == null)
                        return;
                    module.LifeSpan.End = obj.TimeStamp;
                };

                ////////////////////////////////////////////////////////////////
                // .NET methods
                parser.Source.Clr.MethodLoadVerbose += (MethodLoadUnloadVerboseTraceData obj) => {
                    var process = processes
                        .SingleOrDefault(p => p.Id == obj.ProcessID);
                    if (process == null)
                        return;

                    var module = process.Modules
                        .OfType<DotNetModule>()
                        .SingleOrDefault(m => m.Id == obj.ModuleID);
                    if (module == null)
                    {
                        var clrHelpersModule = new DotNetModule("$CLRHelpers$", DefaultModuleChecksum, 0);
                        process.Modules.Add(clrHelpersModule);
                        module = clrHelpersModule;
                    }

                    var method = module.Methods
                        .SingleOrDefault(m => m.Id == obj.MethodID);
                    if (method == null)
                    {
                        method = new DotNetMethod {
                            Id = obj.MethodID,
                            AddressSpace = new AddressSpace(obj.MethodStartAddress, (uint)obj.MethodSize),
                            IsDynamic = obj.IsDynamic,
                            IsGeneric = obj.IsGeneric,
                            IsJitted = obj.IsJitted,
                            Name = obj.MethodName,
                            Namespace = obj.MethodNamespace,
                        };
                        method.LifeSpan.Start = obj.TimeStamp;

                        module.Methods.Add(method);
                    }
                };
                parser.Source.Clr.MethodUnloadVerbose += (MethodLoadUnloadVerboseTraceData obj) => {
                    var process = processes.SingleOrDefault(p => p.Id == obj.ProcessID);
                    if (process == null)
                        return;

                    var module = process.Modules
                        .OfType<DotNetModule>()
                        .SingleOrDefault(m => m.Id == obj.ModuleID);
                    if (module == null)
                        return;

                    var method = module.Methods
                        .SingleOrDefault(m => m.Id == obj.MethodID);
                    if (method == null)
                        return;

                    method.LifeSpan.End = obj.TimeStamp;
                };

                source.Process();

                // Map PMC to managed modules.
                for (int i = pmcSamples.Count - 1; i >= 0; i--)
                {
                    var pmc = pmcSamples[i];

                    var performanceMonitorCounter = scenarioExecutionResult.PerformanceMonitorCounters
                            .SingleOrDefault(p => p.Id == pmc.ProfileSourceId);
                    if (performanceMonitorCounter == null)
                        continue;

                    processes
                        .Single(p => p.Id == pmc.ProcessId).Modules
                        .OfType<DotNetModule>()
                        .ForEach(module => {
                            var methodsCount = module.Methods
                                .Where(m => {
                                    return m.LifeSpan.IsInInterval(pmc.TimeStamp) == 0
                                        && m.AddressSpace.IsInInterval(pmc.InstructionPointer) == 0;
                                })
                                .Count();
                            if (methodsCount != 0)
                            {
                                if (!module.PerformanceMonitorCounterData.ContainsKey(performanceMonitorCounter))
                                    module.PerformanceMonitorCounterData.Add(performanceMonitorCounter, 0);
                                module.PerformanceMonitorCounterData[performanceMonitorCounter] += pmc.SamplingInterval;
                                pmcSamples.RemoveAt(i);
                            }
                        });
                }

                // Map PMC to Unknown module.
                pmcSamples
                    .GroupBy(pmc => pmc.ProcessId)
                    .Select(g1 => {
                        return new {
                            ProcessId = g1.Key,
                            PerformanceMonitorCounters = g1
                                .GroupBy(pmc => pmc.ProfileSourceId)
                                .ToDictionary(
                                    g2 => scenarioExecutionResult.PerformanceMonitorCounters.Single(pmc => pmc.Id == g2.Key),
                                    g2 => g2.Aggregate(0, (long count, PmcSample pmcSample) => count + pmcSample.SamplingInterval)),
                        };
                    })
                    .ForEach(pmcRollover => {
                        var process = processes.Single(p => p.Id == pmcRollover.ProcessId);
                        process.Modules.Add(new Module(UnknownModuleName, DefaultModuleChecksum) {
                            PerformanceMonitorCounterData = pmcRollover.PerformanceMonitorCounters,
                        });
                    });

                return processes;
            }
        }
    }

    static class Extensions
    {
        internal static bool Equivalent(this ImageLoadTraceData @this, Module module)
        {
            if (module.AddressSpace != null)
            {
                return module.AddressSpace.Start == @this.ImageBase
                    && module.AddressSpace.Size == @this.ImageSize
                    && module.Checksum == @this.ImageChecksum
                    && module.FullName == @this.FileName;
            }
            else
            {
                // For example, 'Anonymously Hosted DynamicMethods Assembly'
                return module.Checksum == @this.ImageChecksum && module.FullName == @this.FileName;
            }
        }

        internal static bool IsInTimeAndAddressIntervals(this PMCCounterProfTraceData @this, Module module)
        {
            return module.LifeSpan.IsInInterval(@this.TimeStamp) == 0
                && module.AddressSpace != null // For example, 'Anonymously Hosted DynamicMethods Assembly'
                && module.AddressSpace.IsInInterval(@this.InstructionPointer) == 0;
        }

        /// <summary>
        /// Creates a new object that is a deep copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a deep copy of this instance.</returns>
        internal static Module Copy(this Module @this)
        {
            var newModule = new Module(@this.FullName, @this.Checksum) {
                AddressSpace = @this.AddressSpace,
                PerformanceMonitorCounterData = new Dictionary<PerformanceMonitorCounter, long>(@this.PerformanceMonitorCounterData),
            };

            newModule.LifeSpan.Start = @this.LifeSpan.Start;
            newModule.LifeSpan.End = @this.LifeSpan.End;

            return newModule;
        }
    }
}
