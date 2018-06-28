// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

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
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException();

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

                const int DefaultModuleChecksum = 0;

                ////////////////////////////////////////////////////////////////
                // Process data
                var parser = new KernelTraceEventParser(source);
                parser.ProcessStart += (ProcessTraceData obj) =>
                {
                    if (IsOurProcess(obj))
                    {
                        var process = new Process(obj.ImageFileName, obj.ProcessID, obj.ParentID, scenarioExecutionResult.PerformanceMonitorCounters);
                        process.LifeSpan.Start = obj.TimeStamp;
                        processes.Add(process);
                    }
                };
                parser.ProcessStop += (ProcessTraceData obj) =>
                {
                    if (IsOurProcess(obj))
                        processes.Single(process => process.Id == obj.ProcessID).LifeSpan.End = obj.TimeStamp;
                };

                ////////////////////////////////////////////////////////////////
                // Image/Module data
                parser.ImageLoad += (ImageLoadTraceData obj) =>
                {
                    var process = processes.SingleOrDefault(p => p.Id == obj.ProcessID);
                    if (process == null)
                        return;

                    var module = process.Modules
                        .SingleOrDefault(m => m.Checksum == obj.ImageChecksum && m.FullName == obj.FileName);
                    if (module == null)
                    {
                        module = new Module(obj.FileName, obj.ImageChecksum, scenarioExecutionResult.PerformanceMonitorCounters);
                        process.Modules.Add(module);
                    }
                    module.RuntimeInstances.Add(
                        new RuntimeInstance(new AddressSpace(obj.ImageBase, (uint)obj.ImageSize), obj.TimeStamp));
                };
                parser.ImageUnload += (ImageLoadTraceData obj) =>
                {
                    var process = processes.SingleOrDefault(p => p.Id == obj.ProcessID);
                    if (process == null)
                        return;

                    var module = process.Modules
                        .SingleOrDefault(m => m.Checksum == obj.ImageChecksum && m.FullName == obj.FileName);
                    if (module == null)
                        return;

                    var info = module.RuntimeInstances.SingleOrDefault(i =>
                    {
                        return i.AddressSpace.Start == obj.ImageBase
                            && i.AddressSpace.Size == obj.ImageSize
                            && i.LifeSpan.End > obj.TimeStamp;
                    });
                    if (info == null)
                        return; // Managed methods already unloaded.
                    info.LifeSpan.End = obj.TimeStamp;
                };

                ////////////////////////////////////////////////////////////////
                // "ntoskrnl.exe" data
                parser.ImageDCStart += (ImageLoadTraceData obj) =>
                {
                    if (obj.ProcessID == 0 && obj.FileName.EndsWith("ntoskrnl.exe", StringComparison.Ordinal) && defaultNtoskrnlModule == null)
                    {
                        defaultNtoskrnlModule = new Module(obj.FileName, obj.ImageChecksum, scenarioExecutionResult.PerformanceMonitorCounters);
                        defaultNtoskrnlModule.RuntimeInstances.Add(
                            new RuntimeInstance(new AddressSpace(obj.ImageBase, (uint)obj.ImageSize), obj.TimeStamp));
                    }
                };

                ////////////////////////////////////////////////////////////////
                // PMC data
                var pmcSamples = new List<PmcSample>();
                parser.PerfInfoCollectionStart += (SampledProfileIntervalTraceData obj) =>
                {
                    // Update the Pmc intervals.
                    if (scenarioExecutionResult.PerformanceMonitorCounters.Any(pmc => pmc.Id == obj.SampleSource))
                    {
                        if (!pmcSamplingIntervals.ContainsKey(obj.SampleSource))
                            pmcSamplingIntervals.Add(obj.SampleSource, obj.NewInterval);
                        else
                            pmcSamplingIntervals[obj.SampleSource] = obj.NewInterval;
                    }
                };
                parser.PerfInfoPMCSample += (PMCCounterProfTraceData obj) =>
                {
                    var performanceMonitorCounter = scenarioExecutionResult.PerformanceMonitorCounters
                        .SingleOrDefault(pmc => pmc.Id == obj.ProfileSource);
                    if (performanceMonitorCounter == null)
                        return;

                    var process = processes.SingleOrDefault(p => p.Id == obj.ProcessID);
                    if (process == null)
                        return;

                    process.PerformanceMonitorCounterData[performanceMonitorCounter] += pmcSamplingIntervals[obj.ProfileSource];

                    // Is the IP in the kernel (Under this process)?
                    if (defaultNtoskrnlModule != null && obj.IsInTimeAndAddressIntervals(defaultNtoskrnlModule))
                    {
                        var krnlModule = process.Modules
                            .SingleOrDefault(m => m.Checksum == defaultNtoskrnlModule.Checksum && m.FullName == defaultNtoskrnlModule.FullName);
                        if (krnlModule == null)
                        {
                            krnlModule = new Module(defaultNtoskrnlModule);
                            process.Modules.Add(krnlModule);
                        }
                    }

                    var modules = process.Modules
                        .Where(obj.IsInTimeAndAddressIntervals)
                        .Select(m => m);

                    if (!modules.Any())
                    {
                        // This might fall in managed code. We need to buffer and test it afterwards.
                        pmcSamples.Add(new PmcSample
                        {
                            InstructionPointer = obj.InstructionPointer,
                            ProcessId = obj.ProcessID,
                            ProfileSourceId = obj.ProfileSource,
                            TimeStamp = obj.TimeStamp,
                            SamplingInterval = pmcSamplingIntervals[obj.ProfileSource],
                        });
                        return;
                    }

                    modules.ForEach(module =>
                    {
                        module.PerformanceMonitorCounterData[performanceMonitorCounter] += pmcSamplingIntervals[obj.ProfileSource];
                    });
                };

                ////////////////////////////////////////////////////////////////
                // .NET modules
                parser.Source.Clr.LoaderModuleLoad += (ModuleLoadUnloadTraceData obj) =>
                {
                    var process = processes.SingleOrDefault(p => p.Id == obj.ProcessID);
                    if (process == null)
                        return;

                    var modulePath = string.IsNullOrEmpty(obj.ModuleNativePath) ? obj.ModuleILPath : obj.ModuleNativePath;
                    var module = process.Modules
                        .SingleOrDefault(m => m.FullName == modulePath && m.RuntimeInstances.Any(i => i.LifeSpan.IsInInterval(obj.TimeStamp) == 0));
                    if (module == null)
                    {
                        // Not previously loaded (For example, 'Anonymously Hosted DynamicMethods Assembly')
                        module = new DotNetModule(modulePath, DefaultModuleChecksum, scenarioExecutionResult.PerformanceMonitorCounters, obj.ModuleID);
                        process.Modules.Add(module);
                    }
                    else
                    {
                        // Update/Swap the module. It is a .NET module.
                        var dotnetModule = new DotNetModule(module, obj.ModuleID);
                        process.Modules.Remove(module);     // Remove existing
                        process.Modules.Add(dotnetModule);  // Add it back as managed
                    }
                };
                parser.Source.Clr.LoaderModuleUnload += (ModuleLoadUnloadTraceData obj) =>
                {
                    var process = processes
                        .SingleOrDefault(p => p.Id == obj.ProcessID);
                    if (process == null)
                        return;

                    var module = process.Modules
                        .OfType<DotNetModule>()
                        .SingleOrDefault(m => m.Id == obj.ModuleID && m.RuntimeInstances.Count > 0);
                    if (module == null)
                        return;

                    var info = module.RuntimeInstances
                        .SingleOrDefault(i => i.LifeSpan.IsInInterval(obj.TimeStamp) == 0);
                    if (info == null)
                        throw new InvalidOperationException($"Unloading non-loaded .NET module: {(string.IsNullOrEmpty(obj.ModuleNativePath) ? obj.ModuleILPath : obj.ModuleNativePath)}");
                    info.LifeSpan.End = obj.TimeStamp;
                };

                ////////////////////////////////////////////////////////////////
                // .NET methods
                parser.Source.Clr.MethodLoadVerbose += (MethodLoadUnloadVerboseTraceData obj) =>
                {
                    var process = processes
                        .SingleOrDefault(p => p.Id == obj.ProcessID);
                    if (process == null)
                        return;

                    var module = process.Modules
                        .OfType<DotNetModule>()
                        .SingleOrDefault(m => m.Id == obj.ModuleID);
                    if (module == null)
                    {
                        var clrHelpersModule = new DotNetModule("$CLRHelpers$", DefaultModuleChecksum, scenarioExecutionResult.PerformanceMonitorCounters, 0);
                        process.Modules.Add(clrHelpersModule);
                        module = clrHelpersModule;
                    }

                    var method = module.Methods
                        .SingleOrDefault(m => m.Id == obj.MethodID);
                    if (method == null)
                    {
                        method = new DotNetMethod(
                            obj.MethodID,
                            obj.MethodName,
                            obj.MethodNamespace,
                            obj.IsDynamic,
                            obj.IsGeneric,
                            obj.IsJitted
                        );

                        module.Methods.Add(method);
                    }
                    method.RuntimeInstances.Add(
                        new RuntimeInstance(new AddressSpace(obj.MethodStartAddress, (uint)obj.MethodSize), obj.TimeStamp));
                };
                parser.Source.Clr.MethodUnloadVerbose += (MethodLoadUnloadVerboseTraceData obj) =>
                {
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

                    var info = method.RuntimeInstances.SingleOrDefault(i =>
                    {
                        return i.AddressSpace.Start == obj.MethodStartAddress
                            && i.AddressSpace.Size == obj.MethodSize
                            && i.LifeSpan.End > obj.TimeStamp;
                    });
                    if (info == null)
                        throw new InvalidOperationException($"Unloading non-loaded .NET method: {obj.MethodName}");
                    info.LifeSpan.End = obj.TimeStamp;
                };

                source.Process();

                // TODO: We could order modules/methods by timestamp, then by address?

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
                        .ForEach(module =>
                        {
                            var methodsCount = module.Methods
                            .Count(m =>
                            {
                                return m.RuntimeInstances.Any(info => info.LifeSpan.IsInInterval(pmc.TimeStamp) == 0
                                && info.AddressSpace.IsInInterval(pmc.InstructionPointer) == 0);
                            });
                            if (methodsCount != 0)
                            {
                                module.PerformanceMonitorCounterData[performanceMonitorCounter] += pmc.SamplingInterval;
                                pmcSamples.RemoveAt(i);
                            }
                        });
                }

                // Map PMC to Unknown module.
                const string UnknownModuleName = "Unknown";
                pmcSamples
                    .GroupBy(pmc => pmc.ProcessId)
                    .Select(g1 =>
                    {
                        return new
                        {
                            ProcessId = g1.Key,
                            PerformanceMonitorCounters = g1
                                .GroupBy(pmc => pmc.ProfileSourceId)
                                .ToDictionary(
                                    g2 => scenarioExecutionResult.PerformanceMonitorCounters.Single(pmc => pmc.Id == g2.Key),
                                    g2 => g2.Aggregate(0, (long count, PmcSample pmcSample) => count + pmcSample.SamplingInterval)),
                        };
                    })
                    .ForEach(pmcRollover =>
                    {
                        var process = processes.Single(p => p.Id == pmcRollover.ProcessId);
                        var newModule = new Module(UnknownModuleName, DefaultModuleChecksum, scenarioExecutionResult.PerformanceMonitorCounters);
                        pmcRollover.PerformanceMonitorCounters.ForEach(pair =>
                        {
                            newModule.PerformanceMonitorCounterData[pair.Key] = pair.Value;
                        });
                        process.Modules.Add(newModule);
                    });

                return processes;
            }
        }
    }

    static class Extensions
    {
        internal static bool IsInTimeAndAddressIntervals(this PMCCounterProfTraceData @this, Module module) => module.RuntimeInstances
                .Any(i => i.LifeSpan.IsInInterval(@this.TimeStamp) == 0
                    && i.AddressSpace != null // For example, 'Anonymously Hosted DynamicMethods Assembly'
                    && i.AddressSpace.IsInInterval(@this.InstructionPointer) == 0);
    }
}