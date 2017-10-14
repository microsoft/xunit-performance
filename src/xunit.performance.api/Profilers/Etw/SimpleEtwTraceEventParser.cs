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
    public sealed class SimpleEtwTraceEventParser
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
        ///     3. Pmc source intervals were constant during the whole session.
        /// </remarks>
        public IReadOnlyCollection<EtwProcess> GetProfileData(ScenarioExecutionResult scenarioInfo)
        {
            var processes = new List<EtwProcess>();
            EtwModule tmpNtoskrnlModule = null;
            var pmcSourceIntervals = new Dictionary<int, long>();

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
                        var process = new EtwProcess(obj.ImageFileName, obj.ProcessID, obj.ParentID);
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

                    var module = new EtwModule(obj.FileName, obj.ImageChecksum) {
                        AddressSpace = new EtwAddressSpace(obj.ImageBase, obj.ImageSize)
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
                        if (tmpNtoskrnlModule == null)
                            tmpNtoskrnlModule = new EtwModule(obj.FileName, obj.ImageChecksum);

                        // Assuming nothing else has changed, and keeping the list of already measured Pmc.
                        tmpNtoskrnlModule.AddressSpace = new EtwAddressSpace(obj.ImageBase, obj.ImageSize);
                        tmpNtoskrnlModule.LifeSpan.Start = obj.TimeStamp;
                        tmpNtoskrnlModule.LifeSpan.End = DateTime.MaxValue;
                    }
                };
                parser.ImageDCStop += (ImageLoadTraceData obj) => {
                    if (IsNtoskrnlModule(obj)
                        && tmpNtoskrnlModule != null
                        && obj.TimeStamp > tmpNtoskrnlModule.LifeSpan.End
                        && obj.Equal(tmpNtoskrnlModule))
                    {
                        tmpNtoskrnlModule.LifeSpan.End = obj.TimeStamp;
                    }
                };

                ////////////////////////////////////////////////////////////////
                // PMC data
                var pmcRollovers = new List<EtwPerfInfoPMCSample>();
                parser.PerfInfoCollectionStart += (SampledProfileIntervalTraceData obj) => {
                    // Update the Pmc intervals.
                    if (scenarioInfo.PerformanceMonitorCounters.Any(pmc => pmc.Id == obj.SampleSource))
                    {
                        if (!pmcSourceIntervals.ContainsKey(obj.SampleSource))
                            pmcSourceIntervals.Add(obj.SampleSource, obj.NewInterval);
                        else
                            pmcSourceIntervals[obj.SampleSource] = obj.NewInterval;
                    }
                };
                parser.PerfInfoPMCSample += (PMCCounterProfTraceData obj) => {
                    var process = processes.SingleOrDefault(p => p.Id == obj.ProcessID);
                    if (process == null || !scenarioInfo.PerformanceMonitorCounters.Any(pmc => pmc.Id == obj.ProfileSource))
                        return;

                    // FIXME: How is it possible to get Pmc timestamps greater than my process.ExitTime?
                    if (!process.LifeSpan.IsInInterval(obj.TimeStamp))
                    {
                        pmcRollovers.Add(new EtwPerfInfoPMCSample {
                            InstructionPointer = obj.InstructionPointer,
                            ProcessId = obj.ProcessID,
                            ProfileSourceId = obj.ProfileSource,
                            TimeStamp = obj.TimeStamp,
                        });
                        return;
                    }

                    // If this is our process and it is a Pmc we care to measure.
                    if (!process.PerformanceMonitorCounterData.ContainsKey(obj.ProfileSource))
                        process.PerformanceMonitorCounterData.Add(obj.ProfileSource, 0);
                    process.PerformanceMonitorCounterData[obj.ProfileSource] += pmcSourceIntervals[obj.ProfileSource];

                    // Is the IP in the kernel (Under this process)?
                    if (tmpNtoskrnlModule != null && obj.IsInTimeAndAddressIntervals(tmpNtoskrnlModule))
                    {
                        var krnlModule = process.Modules.SingleOrDefault(m => m.Checksum == tmpNtoskrnlModule.Checksum
                            && m.AddressSpace == tmpNtoskrnlModule.AddressSpace
                            && m.FullName == tmpNtoskrnlModule.FullName);
                        if (krnlModule == null)
                        {
                            krnlModule = tmpNtoskrnlModule.Copy();
                            process.Modules.Add(krnlModule);
                        }

                        //if (!krnlModule.PerformanceMonitorCounterData.ContainsKey(obj.ProfileSource))
                        //    krnlModule.PerformanceMonitorCounterData.Add(obj.ProfileSource, 0);
                        //krnlModule.PerformanceMonitorCounterData[obj.ProfileSource] += pmcSourceIntervals[obj.ProfileSource];
                        //return;
                    }

                    var modules = process.Modules
                        .Where(m => {
                            return obj.IsInTimeAndAddressIntervals(m);
                        })
                        .Select(m => m);

                    if (modules.Count() == 0)
                    {
                        // This might fall in managed code. We need to buffer and test it afterwards.
                        pmcRollovers.Add(new EtwPerfInfoPMCSample {
                            InstructionPointer = obj.InstructionPointer,
                            ProcessId = obj.ProcessID,
                            ProfileSourceId = obj.ProfileSource,
                            TimeStamp = obj.TimeStamp,
                        });
                        return;
                    }

                    modules.ForEach(module => {
                        if (!module.PerformanceMonitorCounterData.ContainsKey(obj.ProfileSource))
                            module.PerformanceMonitorCounterData.Add(obj.ProfileSource, 0);
                        module.PerformanceMonitorCounterData[obj.ProfileSource] += pmcSourceIntervals[obj.ProfileSource];
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
                        module = new EtwDotNetModule(obj.ModuleILPath, AnonymouslyHostedDynamicMethodsAssemblyChecksum, obj.ModuleID);
                        process.Modules.Add(module);
                    }
                    else
                    {
                        // Update/Swap the module. It is a .NET module.
                        var dotnetModule = new EtwDotNetModule(module.FullName, module.Checksum, obj.ModuleID) {
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
                        .OfType<EtwDotNetModule>()
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
                        .OfType<EtwDotNetModule>()
                        .SingleOrDefault(m => m.Id == obj.ModuleID);
                    if (module == null)
                        throw new InvalidOperationException($"Method for non-loaded module found! ModuleId: {obj.ModuleID}, MethodName: {obj.MethodName}");

                    var method = module.Methods
                        .SingleOrDefault(m => m.Id == obj.MethodID);
                    if (method == null)
                    {
                        method = new EtwDotNetMethod {
                            Id = obj.MethodID,
                            AddressSpace = new EtwAddressSpace(obj.MethodStartAddress, obj.MethodSize),
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
                        .OfType<EtwDotNetModule>()
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
                        processes
                            .Single(p => p.Id == pmc.ProcessId).Modules
                            .OfType<EtwDotNetModule>()
                            .ForEach(module => {
                                var isInModule = module.AddressSpace != null
                                    && module.LifeSpan.IsInInterval(pmc.TimeStamp)
                                    && module.AddressSpace.IsInInterval(pmc.InstructionPointer);
                                if (isInModule)
                                {
                                    if (!module.PerformanceMonitorCounterData.ContainsKey(pmc.ProfileSourceId))
                                        module.PerformanceMonitorCounterData.Add(pmc.ProfileSourceId, 0);
                                    module.PerformanceMonitorCounterData[pmc.ProfileSourceId] += pmcSourceIntervals[pmc.ProfileSourceId];
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
                                    if (!module.PerformanceMonitorCounterData.ContainsKey(pmc.ProfileSourceId))
                                        module.PerformanceMonitorCounterData.Add(pmc.ProfileSourceId, 0);
                                    module.PerformanceMonitorCounterData[pmc.ProfileSourceId] += pmcSourceIntervals[pmc.ProfileSourceId];
                                    pmcRollovers.Remove(pmc);
                                }
                            });
                    });

                // Map PMC to managed and Unknown module.
                const string UnknownModuleName = "Unknown";
                pmcRollovers
                    .ForEach(pmc => {
                        var process = processes
                            .SingleOrDefault(p => p.Id == pmc.ProcessId && p.LifeSpan.IsInInterval(pmc.TimeStamp));
                        if (process == null)
                            return;

                        var unknownModule = process.Modules
                            .SingleOrDefault(m => m.FullName == UnknownModuleName);

                        if (unknownModule == null)
                        {
                            unknownModule = new EtwModule(UnknownModuleName, 0);
                            process.Modules.Add(unknownModule);
                        }

                        if (!unknownModule.PerformanceMonitorCounterData.ContainsKey(pmc.ProfileSourceId))
                            unknownModule.PerformanceMonitorCounterData.Add(pmc.ProfileSourceId, 0);
                        unknownModule.PerformanceMonitorCounterData[pmc.ProfileSourceId] += pmcSourceIntervals[pmc.ProfileSourceId];
                        pmcRollovers.Remove(pmc);
                    });

                return processes;
            }
        }
    }

    static class Extensions
    {
        public static bool Equal(this ImageLoadTraceData @this, EtwModule module)
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

        public static bool IsInTimeAndAddressIntervals(this PMCCounterProfTraceData @this, EtwModule module)
        {
            return module.LifeSpan.IsInInterval(@this.TimeStamp)
                && module.AddressSpace != null // For example, 'Anonymously Hosted DynamicMethods Assembly'
                && module.AddressSpace.IsInInterval(@this.InstructionPointer);
        }
    }
}
