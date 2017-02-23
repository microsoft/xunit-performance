using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Xunit.Performance.Api.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using static Microsoft.Xunit.Performance.Api.Native.Windows.Kernel32;
using static Microsoft.Xunit.Performance.Api.XunitPerformanceLogger;

namespace Microsoft.Xunit.Performance.Api
{
    internal static class ETWProfiler
    {
        /// <summary>
        ///     1. In the specified assembly, get the ETW providers set as assembly attributes (PerformanceTestInfo)
        ///     2. Check if the benchmark assembly request Precise Machine Counters(PMC) to be collected
        ///     3. Enable Kernel providers if needed
        ///     4. Get non-kernel ETW flags set and enable them
        ///     5. Run the benchmarks
        ///     6. Stop collecting ETW
        ///     7. Merge ETL files.
        /// </summary>
        /// <param name="assemblyFileName"></param>
        /// <param name="sessionName"></param>
        /// <param name="outputDirectory"></param>
        /// <param name="action"></param>
        /// <param name="collectOutputFilesCallback">Callback used to collect a list of files generated.</param>
        /// <returns></returns>
        public static void Record(string assemblyFileName, string sessionName, string outputDirectory, Action action, Action<string> collectOutputFilesCallback)
        {
            const int bufferSizeMB = 128;
            var sessionFileName = $"{sessionName}-{Path.GetFileNameWithoutExtension(assemblyFileName)}";
            var userFullFileName = Path.Combine(outputDirectory, $"{sessionFileName}.etl");
            var kernelFullFileName = Path.Combine(outputDirectory, $"{sessionFileName}.kernel.etl"); // without this parameter, EnableKernelProvider will fail

            PrintProfilingInformation(assemblyFileName, sessionName, userFullFileName);

            (var providers, var performanceTestMessages) = XunitBenchmark.GetMetadata(assemblyFileName);
            var kernelProviderInfo = providers.OfType<KernelProviderInfo>().FirstOrDefault();

            var needKernelSession = NeedSeparateKernelSession(kernelProviderInfo);
            using (var kernelSession = needKernelSession ? new TraceEventSession(KernelTraceEventParser.KernelSessionName, kernelFullFileName) : null)
            {
                if (kernelSession != null)
                {
                    SetPreciseMachineCounters(providers);
                    kernelSession.BufferSizeMB = bufferSizeMB;
                    var flags = (KernelTraceEventParser.Keywords)kernelProviderInfo.Keywords;
                    var stackCapture = (KernelTraceEventParser.Keywords)kernelProviderInfo.StackKeywords;
                    kernelSession.EnableKernelProvider(flags, stackCapture);
                }

                using (var userEventSession = new TraceEventSession(sessionName, userFullFileName))
                {
                    userEventSession.BufferSizeMB = bufferSizeMB;

                    var flags = KernelTraceEventParser.Keywords.Process | KernelTraceEventParser.Keywords.ImageLoad | KernelTraceEventParser.Keywords.Thread;
                    var stackCapture = KernelTraceEventParser.Keywords.Profile | KernelTraceEventParser.Keywords.ContextSwitch;
                    userEventSession.EnableKernelProvider(flags, stackCapture);

                    foreach (var userProviderInfo in providers.OfType<UserProviderInfo>())
                        userEventSession.EnableProvider(userProviderInfo.ProviderGuid, userProviderInfo.Level, userProviderInfo.Keywords);

                    action.Invoke();
                }
            }

            TraceEventSession.MergeInPlace(userFullFileName, Console.Out);
            WriteInfoLine($"ETW Tracing Session saved to \"{userFullFileName}\"");
            collectOutputFilesCallback(userFullFileName);

            var assemblyModel = GetAssemblyModel(assemblyFileName, userFullFileName, sessionName, performanceTestMessages);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(userFullFileName);
            var xmlFileName = Path.Combine(outputDirectory, $"{fileNameWithoutExtension}.xml");
            new AssemblyModelCollection { assemblyModel }.Serialize(xmlFileName);
            WriteInfoLine($"Performance results saved to \"{xmlFileName}\"");
            collectOutputFilesCallback(xmlFileName);

            var mdFileName = Path.Combine(outputDirectory, $"{fileNameWithoutExtension}.md");
            var dt = assemblyModel.GetStatistics();
            var mdTable = MarkdownHelper.GenerateMarkdownTable(dt);
            MarkdownHelper.Write(mdFileName, mdTable);
            WriteInfoLine($"Markdown file saved to \"{mdFileName}\"");
            collectOutputFilesCallback(mdFileName);
            Console.WriteLine(mdTable);

            var csvFileName = Path.Combine(outputDirectory, $"{fileNameWithoutExtension}.csv");
            dt.WriteToCSV(csvFileName);
            WriteInfoLine($"Statistics written to \"{csvFileName}\"");
            collectOutputFilesCallback(csvFileName);
        }

        private static AssemblyModel GetAssemblyModel(
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

        private static EtwPerformanceMetricEvaluationContext GetEtwReader(
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

        private static void SetPreciseMachineCounters(IEnumerable<ProviderInfo> providers)
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

        private static bool NeedSeparateKernelSession(KernelProviderInfo kernelProviderInfo)
        {
            if (kernelProviderInfo == null)
                return false;

            // Prior to Windows 8 (NT 6.2), all kernel events needed the special kernel session.
            if (!IsWindows8OrGreater)
                return true;

            // CPU counters need the special kernel session
            var keywords = (KernelTraceEventParser.Keywords)kernelProviderInfo.Keywords & KernelTraceEventParser.Keywords.PMCProfile;
            return (keywords != 0);
        }

        private static bool IsWindows8OrGreater => IsWindows8OrGreater();

        [Conditional("DEBUG")]
        private static void PrintProfilingInformation(string assemblyFileName, string sessionName, string userFullFileName)
        {
            WriteDebugLine("  ===== ETW Profiling information =====");
            WriteDebugLine($"       Assembly: {assemblyFileName}");
            WriteDebugLine($"     Process Id: {Process.GetCurrentProcess().Id}");
            WriteDebugLine($"   Session name: {sessionName}");
            WriteDebugLine($"  ETW file name: {userFullFileName}");
            WriteDebugLine($"  Provider guid: {MicrosoftXunitBenchmarkTraceEventParser.ProviderGuid}");
            WriteDebugLine($"  Provider name: {MicrosoftXunitBenchmarkTraceEventParser.ProviderName}");
            WriteDebugLine("  =====================================");
        }

        [Conditional("DEBUG")]
        private static void PrintAvailableProfileSources()
        {
            var availableProfileSources = TraceEventProfileSources.GetInfo();
            var cpuCounterIds = new List<int>();
            var cpuCounterIntervals = new List<int>();
            var sb = new StringBuilder();

            foreach (var kvp in availableProfileSources)
            {
                sb.AppendLine();
                sb.AppendLine($"Profile name: {kvp.Key}");
                sb.AppendLine($"  ID :          {kvp.Value.ID}");
                sb.AppendLine($"  Interval :    {kvp.Value.Interval}");
                sb.AppendLine($"  MaxInterval : {kvp.Value.MaxInterval}");
                sb.AppendLine($"  MinInterval : {kvp.Value.MinInterval}");
                sb.AppendLine();
            }

            WriteDebugLine(sb.ToString());
        }
    }
}
