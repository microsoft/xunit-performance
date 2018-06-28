using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Xunit.Performance.Api.Profilers.Etw;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static Microsoft.Xunit.Performance.Api.Native.Windows.Kernel32;
using static Microsoft.Xunit.Performance.Api.PerformanceLogger;

namespace Microsoft.Xunit.Performance.Api
{
    static class ETWProfiler
    {
        public static bool CanEnableEnableKernelProvider => TraceEventSession.IsElevated() == true;

        static bool IsWindows8OrGreater => IsWindows8OrGreater();

        /// <summary>
        ///     1. In the specified assembly, get the ETW providers set as assembly attributes (PerformanceTestInfo)
        ///     2. Check if the benchmark assembly request Precise Machine Counters(PMC) to be collected
        ///     3. Enable Kernel providers if needed
        ///     4. Get non-kernel ETW flags set and enable them
        ///     5. Run the benchmarks
        ///     6. Stop collecting ETW
        ///     7. Merge ETL files.
        /// </summary>
        /// <param name="xUnitPerformanceSessionData"></param>
        /// <param name="xUnitPerformanceMetricData"></param>
        /// <param name="action"></param>
        public static void Record(XUnitPerformanceSessionData xUnitPerformanceSessionData, XUnitPerformanceMetricData xUnitPerformanceMetricData, Action action)
        {
            const int bufferSizeMB = 512;
            var name = $"{xUnitPerformanceSessionData.RunId}-{Path.GetFileNameWithoutExtension(xUnitPerformanceSessionData.AssemblyFileName)}";
            var etwOutputData = new ETWOutputData
            {
                KernelFileName = Path.Combine(xUnitPerformanceSessionData.OutputDirectory, $"{name}.kernel.etl"), // without this parameter, EnableKernelProvider will fail
                Name = name,
                SessionName = $"Performance-Api-Session-{xUnitPerformanceSessionData.RunId}",
                UserFileName = Path.Combine(xUnitPerformanceSessionData.OutputDirectory, $"{name}.etl"),
            };

            PrintProfilingInformation(xUnitPerformanceSessionData.AssemblyFileName, etwOutputData);

            var kernelProviderInfo = xUnitPerformanceMetricData.Providers.OfType<KernelProviderInfo>().FirstOrDefault();
            var needKernelSession = NeedSeparateKernelSession(kernelProviderInfo);

            if (needKernelSession && !CanEnableEnableKernelProvider)
            {
                const string message = "In order to capture kernel data the application is required to run as Administrator.";
                WriteErrorLine(message);
                throw new InvalidOperationException(message);
            }

            WriteDebugLine("> ETW capture start.");

            // Prior to Windows 8 (NT 6.2), all kernel events needed the special kernel session.
            using (var safeKernelSession = needKernelSession && CanEnableEnableKernelProvider ? MakeSafeTerminateTraceEventSession(KernelTraceEventParser.KernelSessionName, etwOutputData.KernelFileName) : null)
            {
                var kernelSession = safeKernelSession?.BaseDisposableObject;
                if (kernelSession != null)
                {
                    SetPreciseMachineCounters(xUnitPerformanceMetricData.Providers);
                    kernelSession.BufferSizeMB = bufferSizeMB;
                    var flags = (KernelTraceEventParser.Keywords)kernelProviderInfo.Keywords;
                    var stackCapture = (KernelTraceEventParser.Keywords)kernelProviderInfo.StackKeywords;
                    kernelSession.EnableKernelProvider(flags, stackCapture);
                }

                using (var safeUserEventSession = MakeSafeTerminateTraceEventSession(etwOutputData.SessionName, etwOutputData.UserFileName))
                {
                    var userEventSession = safeUserEventSession.BaseDisposableObject;
                    userEventSession.BufferSizeMB = bufferSizeMB;

                    if (needKernelSession && CanEnableEnableKernelProvider)
                        userEventSession.EnableKernelProvider(KernelProvider.Default.Flags, KernelProvider.Default.StackCapture);

                    foreach (var provider in UserProvider.Defaults)
                        userEventSession.EnableProvider(provider.Guid, provider.Level, provider.Keywords);
                    foreach (var provider in xUnitPerformanceMetricData.Providers.OfType<UserProviderInfo>())
                        userEventSession.EnableProvider(provider.ProviderGuid, provider.Level, provider.Keywords);

                    action.Invoke();
                }
            }

            WriteDebugLine("> ETW capture stop.");

            // TODO: Decouple the code below.
            // Collect ETW profile data.
            //  TODO: Skip collecting kernel data if it was not captured! (data will be zero, there is no point to report it or upload it)
            WriteDebugLine("> ETW merge start.");
            TraceEventSession.MergeInPlace(etwOutputData.UserFileName, Console.Out);
            WriteDebugLine("> ETW merge stop.");
            xUnitPerformanceSessionData.CollectOutputFilesCallback(etwOutputData.UserFileName);

            var assemblyModel = GetAssemblyModel(xUnitPerformanceSessionData.AssemblyFileName, etwOutputData.UserFileName, xUnitPerformanceSessionData.RunId, xUnitPerformanceMetricData.PerformanceTestMessages);
            var xmlFileName = Path.Combine(xUnitPerformanceSessionData.OutputDirectory, $"{etwOutputData.Name}.xml");
            new AssemblyModelCollection { assemblyModel }.Serialize(xmlFileName);
            xUnitPerformanceSessionData.CollectOutputFilesCallback(xmlFileName);

            var mdFileName = Path.Combine(xUnitPerformanceSessionData.OutputDirectory, $"{etwOutputData.Name}.md");
            var dt = assemblyModel.GetStatistics();
            var mdTable = MarkdownHelper.GenerateMarkdownTable(dt);
            MarkdownHelper.Write(mdFileName, mdTable);
            xUnitPerformanceSessionData.CollectOutputFilesCallback(mdFileName);

            Console.WriteLine(MarkdownHelper.ToTrimmedTable(mdTable));

            var csvFileName = Path.Combine(xUnitPerformanceSessionData.OutputDirectory, $"{etwOutputData.Name}.csv");
            dt.WriteToCSV(csvFileName);
            xUnitPerformanceSessionData.CollectOutputFilesCallback(csvFileName);
        }

        static AssemblyModel GetAssemblyModel(
            string assemblyFileName,
            string etlFileName,
            string sessionName,
            IEnumerable<PerformanceTestMessage> performanceTestMessages)
        {
            using (var reader = GetEtwReader(etlFileName, sessionName, performanceTestMessages))
            {
                var assemblyModel = new AssemblyModel
                {
                    Name = Path.GetFileName(assemblyFileName),
                    Collection = new List<TestModel>()
                };

                foreach (var test in performanceTestMessages)
                {
                    var metrics = new List<MetricModel>();
                    foreach (var metric in test.Metrics)
                    {
                        metrics.Add(new MetricModel
                        {
                            DisplayName = metric.DisplayName,
                            Name = metric.Id,
                            Unit = metric.Unit,
                        });
                    }

                    var testModel = new TestModel
                    {
                        Name = test.TestCase.DisplayName,
                        Method = test.TestCase.TestMethod.Method.Name,
                        ClassName = test.TestCase.TestMethod.TestClass.Class.Name,
                        Performance = new PerformanceModel { Metrics = metrics, IterationModels = new List<IterationModel>() },
                    };

                    var values = reader.GetValues(testModel.Name);
                    if (values == null)
                        continue; // If the test was not run, then it will not be found on the trace (e.g. user only ran a subset of all tests).
                    foreach (var dict in values)
                    {
                        var iterationModel = new IterationModel { Iteration = new Dictionary<string, double>() };
                        foreach (var kvp in dict)
                            iterationModel.Iteration.Add(kvp.Key, kvp.Value);
                        if (iterationModel.Iteration.Count > 0)
                            testModel.Performance.IterationModels.Add(iterationModel);
                    }

                    assemblyModel.Collection.Add(testModel);
                }

                return assemblyModel;
            }
        }

        static EtwPerformanceMetricEvaluationContext GetEtwReader(
            string fileName,
            string sessionName,
            IEnumerable<PerformanceTestMessage> performanceTestMessages)
        {
            using (var source = new ETWTraceEventSource(fileName))
            {
                if (source.EventsLost > 0)
                    throw new Exception($"Events were lost in trace '{fileName}'");

                using (var context = new EtwPerformanceMetricEvaluationContext(
                    fileName, source, performanceTestMessages, sessionName))
                {
                    source.Process();
                    return context;
                }
            }
        }

        [Conditional("DEBUG")]
        static void GetRegisteredProvidersInProcess() => TraceEventProviders.GetRegisteredProvidersInProcess(System.Diagnostics.Process.GetCurrentProcess().Id)
                .Select(TraceEventProviders.GetProviderName)
                .ForEach(name => Debug.WriteLine(name));

        static SafeTerminateHandler<TraceEventSession> MakeSafeTerminateTraceEventSession(string sessionName, string fileName) => new SafeTerminateHandler<TraceEventSession>(() => new TraceEventSession(sessionName, fileName));

        static bool NeedSeparateKernelSession(KernelProviderInfo kernelProviderInfo)
        {
            if (kernelProviderInfo == null)
                return false;

            // CPU counters need the special kernel session
            var keywords = (KernelTraceEventParser.Keywords)kernelProviderInfo.Keywords
                & (KernelTraceEventParser.Keywords.Profile | KernelTraceEventParser.Keywords.PMCProfile);
            return (keywords != KernelTraceEventParser.Keywords.None);
        }

        [Conditional("DEBUG")]
        static void PrintAvailableProfileSources()
        {
            var availableProfileSources = TraceEventProfileSources.GetInfo();

            foreach (var kvp in availableProfileSources)
            {
                Debug.WriteLine("");
                Debug.WriteLine($"Profile name: {kvp.Key}");
                Debug.WriteLine($"  ID :          {kvp.Value.ID}");
                Debug.WriteLine($"  Interval :    {kvp.Value.Interval}");
                Debug.WriteLine($"  MaxInterval : {kvp.Value.MaxInterval}");
                Debug.WriteLine($"  MinInterval : {kvp.Value.MinInterval}");
                Debug.WriteLine("");
            }
        }

        [Conditional("DEBUG")]
        static void PrintProfilingInformation(string assemblyFileName, ETWOutputData etwOutputData)
        {
            WriteDebugLine("  ===== ETW Profiling information =====");
            WriteDebugLine($"       Assembly: {assemblyFileName}");
            WriteDebugLine($"     Process Id: {System.Diagnostics.Process.GetCurrentProcess().Id}");
            WriteDebugLine($"   Session name: {etwOutputData.SessionName}");
            WriteDebugLine($"  ETW file name: {etwOutputData.UserFileName}");
            WriteDebugLine($"  Provider guid: {MicrosoftXunitBenchmarkTraceEventParser.ProviderGuid}");
            WriteDebugLine($"  Provider name: {MicrosoftXunitBenchmarkTraceEventParser.ProviderName}");
            WriteDebugLine("  =====================================");
        }

        static void SetPreciseMachineCounters(IEnumerable<ProviderInfo> providers)
        {
            if (IsWindows8OrGreater)
            {
                var availableCpuCounters = TraceEventProfileSources.GetInfo();
                var profileSourceIDs = new List<int>();
                var profileSourceIntervals = new List<int>();

                foreach (var cpuInfo in providers.OfType<CpuCounterInfo>())
                {
                    if (availableCpuCounters.TryGetValue(cpuInfo.CounterName, out var profInfo))
                    {
                        profileSourceIDs.Add(profInfo.ID);
                        profileSourceIntervals.Add(Math.Min(profInfo.MaxInterval, Math.Max(profInfo.MinInterval, cpuInfo.Interval)));
                    }
                }

                if (profileSourceIDs.Count > 0)
                {
                    //
                    // FIXME: This function changes the -pmcsources intervals machine wide.
                    //  Maybe we should undo/revert these changes!
                    //
                    TraceEventProfileSources.Set(profileSourceIDs.ToArray(), profileSourceIntervals.ToArray());
                }
            }
        }
    }
}