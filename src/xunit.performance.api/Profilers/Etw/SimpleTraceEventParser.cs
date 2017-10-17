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
        /// Gets profile data from the provided ScenarioInfo object
        /// </summary>
        /// <param name="scenarioInfo"></param>
        /// <returns>A collection of the profiled processes for the given scenario.</returns>
        /// <remarks>
        /// Some assumptions:
        ///     1. The scenario launches a single process, but itself can launch child processes.
        ///     2. Process started/stopped within the ETW session.
        /// </remarks>
        public IReadOnlyCollection<Process> GetProfileData(ScenarioExecutionResult scenarioInfo)
        {
            var processes = new List<Process>();
            Module defaultNtoskrnlModule = null;
            var pmcSamplingIntervals = new Dictionary<int, long>();

            Func<int, bool> IsOurProcess = (processId) => {
                return processId == scenarioInfo.ProcessExitInfo.ProcessId || processes.Any(process => process.Id == processId);
            };

            using (var source = new ETWTraceEventSource(scenarioInfo.EventLogFileName))
            {
                if (source.EventsLost > 0)
                    throw new Exception($"Events lost in trace '{scenarioInfo.EventLogFileName}'");

                ////////////////////////////////////////////////////////////////
                // Process data
                var parser = new KernelTraceEventParser(source);
                parser.ProcessStart += (ProcessTraceData obj) => {
                    if (IsOurProcess(obj.ProcessID) || IsOurProcess(obj.ParentID))
                    {
                        var process = new Process(obj.ImageFileName, obj.ProcessID, obj.ParentID);
                        process.LifeSpan.Start = obj.TimeStamp;
                        processes.Add(process);
                    }
                };
                parser.ProcessStop += (ProcessTraceData obj) => {
                    if (IsOurProcess(obj.ProcessID))
                        processes.Single(process => process.Id == obj.ProcessID).LifeSpan.End = obj.TimeStamp;
                };

                ////////////////////////////////////////////////////////////////
                // Image/Module data
                parser.ImageLoad += (ImageLoadTraceData obj) => {
                    var process = processes.SingleOrDefault(p => p.Id == obj.ProcessID);
                    if (process == null)
                        return;

                    var module = new Module(obj.FileName, obj.ImageChecksum) {
                        AddressSpace = new AddressSpace(obj.ImageBase, obj.ImageSize)
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
                        .SingleOrDefault(m => obj.TimeStamp > m.LifeSpan.End && obj.Equal(m));
                    if (module == null)
                        return;
                    module.LifeSpan.End = obj.TimeStamp;
                };

                ////////////////////////////////////////////////////////////////
                // "ntoskrnl.exe" data
                Func<ImageLoadTraceData, bool> IsNtoskrnlModule = (ImageLoadTraceData obj) => {
                    return obj.ProcessID == 0 && obj.FileName.EndsWith("ntoskrnl.exe");
                };
                parser.ImageDCStart += (ImageLoadTraceData obj) => {
                    if (IsNtoskrnlModule(obj))
                    {
                        if (defaultNtoskrnlModule == null)
                            defaultNtoskrnlModule = new Module(obj.FileName, obj.ImageChecksum);

                        defaultNtoskrnlModule.AddressSpace = new AddressSpace(obj.ImageBase, obj.ImageSize);
                        defaultNtoskrnlModule.LifeSpan.Start = obj.TimeStamp;
                        defaultNtoskrnlModule.LifeSpan.End = DateTime.MaxValue;
                    }
                };
                parser.ImageDCStop += (ImageLoadTraceData obj) => {
                    if (IsNtoskrnlModule(obj)
                        && defaultNtoskrnlModule != null
                        && obj.TimeStamp > defaultNtoskrnlModule.LifeSpan.End
                        && obj.Equal(defaultNtoskrnlModule))
                    {
                        defaultNtoskrnlModule.LifeSpan.End = obj.TimeStamp;
                    }
                };

                ////////////////////////////////////////////////////////////////
                // PMC data
                var pmcRollovers = new List<PmcRollover>();
                parser.PerfInfoCollectionStart += (SampledProfileIntervalTraceData obj) => {
                    // Update the Pmc intervals.
                    if (scenarioInfo.PerformanceMonitorCounters.Any(pmc => pmc.Id == obj.SampleSource))
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

                    var performanceMonitorCounter = scenarioInfo.PerformanceMonitorCounters
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
                        var krnlModule = process.Modules.SingleOrDefault(m => m.Checksum == defaultNtoskrnlModule.Checksum
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
                        pmcRollovers.Add(new PmcRollover {
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

                    // FIXME: Is this condition correct? Will a module be always loaded, on ImageLoad, before getting here?
                    var module = process.Modules
                        .SingleOrDefault(m => m.FullName == obj.ModuleILPath);
                    if (module == null)
                    {
                        // Not previously loaded (For example, 'Anonymously Hosted DynamicMethods Assembly')
                        const int AnonymouslyHostedDynamicMethodsAssemblyChecksum = 0;
                        module = new DotNetModule(obj.ModuleILPath, AnonymouslyHostedDynamicMethodsAssemblyChecksum, obj.ModuleID);
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
                        throw new InvalidOperationException($"Method for non-loaded module found! ModuleId: {obj.ModuleID}, MethodName: {obj.MethodName}");

                    var method = module.Methods
                        .SingleOrDefault(m => m.Id == obj.MethodID);
                    if (method == null)
                    {
                        method = new DotNetMethod {
                            Id = obj.MethodID,
                            AddressSpace = new AddressSpace(obj.MethodStartAddress, obj.MethodSize),
                            IsDynamic = obj.IsDynamic,
                            IsGeneric = obj.IsGeneric,
                            IsJitted = obj.IsJitted,
                            MethodName = obj.MethodName,
                            MethodNamespace = obj.MethodNamespace,
                        };
                        method.LifeSpan.Start = obj.TimeStamp;
                        method.LifeSpan.End = DateTime.MaxValue;

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
                pmcRollovers
                    .ForEach(pmc => {
                        var performanceMonitorCounter = scenarioInfo.PerformanceMonitorCounters
                            .SingleOrDefault(p => p.Id == pmc.ProfileSourceId);
                        if (performanceMonitorCounter == null)
                            return;

                        processes
                            .Single(p => p.Id == pmc.ProcessId).Modules
                            .OfType<DotNetModule>()
                            .ForEach(module => {
                                var isInModule = module.AddressSpace != null
                                    && module.LifeSpan.IsInInterval(pmc.TimeStamp)
                                    && module.AddressSpace.IsInInterval(pmc.InstructionPointer);
                                if (isInModule)
                                {
                                    if (!module.PerformanceMonitorCounterData.ContainsKey(performanceMonitorCounter))
                                        module.PerformanceMonitorCounterData.Add(performanceMonitorCounter, 0);
                                    module.PerformanceMonitorCounterData[performanceMonitorCounter] += pmc.SamplingInterval;
                                    pmcRollovers.Remove(pmc);
                                    return;
                                }

                                var methods = module.Methods
                                    .Where(m => {
                                        return m.LifeSpan.IsInInterval(pmc.TimeStamp)
                                            && m.AddressSpace.IsInInterval(pmc.InstructionPointer);
                                    })
                                    .Select(m => m);
                                if (methods.Count() != 0)
                                {
                                    if (!module.PerformanceMonitorCounterData.ContainsKey(performanceMonitorCounter))
                                        module.PerformanceMonitorCounterData.Add(performanceMonitorCounter, 0);
                                    module.PerformanceMonitorCounterData[performanceMonitorCounter] += pmc.SamplingInterval;
                                    pmcRollovers.Remove(pmc);
                                }
                            });
                    });

                // Map PMC to managed and Unknown module.
                const string UnknownModuleName = "Unknown";
                pmcRollovers
                    .ForEach(pmc => {
                        var performanceMonitorCounter = scenarioInfo.PerformanceMonitorCounters
                            .SingleOrDefault(p => p.Id == pmc.ProfileSourceId);
                        if (performanceMonitorCounter == null)
                            return;

                        var process = processes
                            .SingleOrDefault(p => p.Id == pmc.ProcessId && p.LifeSpan.IsInInterval(pmc.TimeStamp));
                        if (process == null)
                            return;

                        var unknownModule = process.Modules
                            .SingleOrDefault(m => m.FullName == UnknownModuleName);

                        if (unknownModule == null)
                        {
                            unknownModule = new Module(UnknownModuleName, 0);
                            process.Modules.Add(unknownModule);
                        }

                        if (!unknownModule.PerformanceMonitorCounterData.ContainsKey(performanceMonitorCounter))
                            unknownModule.PerformanceMonitorCounterData.Add(performanceMonitorCounter, 0);
                        unknownModule.PerformanceMonitorCounterData[performanceMonitorCounter] += pmc.SamplingInterval;
                        pmcRollovers.Remove(pmc);
                    });

                return processes;
            }
        }
    }

    static class Extensions
    {
        public static bool Equal(this ImageLoadTraceData @this, Module module)
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

        public static bool IsInTimeAndAddressIntervals(this PMCCounterProfTraceData @this, Module module)
        {
            return module.LifeSpan.IsInInterval(@this.TimeStamp)
                && module.AddressSpace != null // For example, 'Anonymously Hosted DynamicMethods Assembly'
                && module.AddressSpace.IsInInterval(@this.InstructionPointer);
        }
    }
}
