using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Xunit.Performance.Sdk;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using Xunit;
using static Microsoft.Xunit.Performance.Api.Native.Windows.Kernel32;
using static Microsoft.Xunit.Performance.Api.XunitPerformanceLogger;

namespace Microsoft.Xunit.Performance.Api
{
    internal static class ETWProfiler
    {
        static ETWProfiler()
        {
            RequiredProviders = new ProviderInfo[]
            {
                new KernelProviderInfo()
                {
                    Keywords = (ulong)KernelTraceEventParser.Keywords.Process | (ulong)KernelTraceEventParser.Keywords.Profile,
                    StackKeywords = (ulong)KernelTraceEventParser.Keywords.Profile
                },
                new CpuCounterInfo()
                {
                    CounterName = "BranchMispredictions",
                    Interval = 100000
                },
                new CpuCounterInfo()
                {
                    CounterName = "CacheMisses",
                    Interval = 100000
                },
                new UserProviderInfo()
                {
                    ProviderGuid = MicrosoftXunitBenchmarkTraceEventParser.ProviderGuid,
                    Level = TraceEventLevel.Verbose,
                    Keywords = ulong.MaxValue,
                },
                new UserProviderInfo()
                {
                    ProviderGuid = ClrTraceEventParser.ProviderGuid,
                    Level = TraceEventLevel.Verbose,
                    Keywords =
                    (
                        //(ulong)ClrTraceEventParser.Keywords.Default // TODO: Should we be using this instead?
                        (ulong)ClrTraceEventParser.Keywords.Jit |
                        (ulong)ClrTraceEventParser.Keywords.JittedMethodILToNativeMap |
                        (ulong)ClrTraceEventParser.Keywords.Loader |
                        (ulong)ClrTraceEventParser.Keywords.Exception |
                        (ulong)ClrTraceEventParser.Keywords.GC
                    ),
                }
            };

            // If !Windows then DO NOT Profile!
            Profile = new ProfileDelegate(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                (ProfileDelegate)ProfileWindows : ProfileNonWindows);
        }

        public delegate void ProfileDelegate(string assemblyFileName, string sessionName, Action action, int bufferSizeMB = 128);

        public static ProfileDelegate Profile { get; }

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
        /// <param name="action"></param>
        /// <param name="bufferSizeMB"></param>
        /// <returns></returns>
        public static void ProfileWindows(string assemblyFileName, string sessionName, Action action, int bufferSizeMB = 128)
        {
            var sessionFileName = $"{sessionName}-{Path.GetFileNameWithoutExtension(assemblyFileName)}";
            var userFileName = $"{sessionFileName}.etl";
            var kernelFileName = $"{sessionFileName}.kernel.etl";
            var currentWorkingDirectory = Directory.GetCurrentDirectory();
            var userFullFileName = Path.Combine(currentWorkingDirectory, userFileName);
            var kernelFullFileName = Path.Combine(currentWorkingDirectory, kernelFileName); /* without this parameter, EnableKernelProvider will fail */

            PrintProfilingInformation(assemblyFileName, sessionName, userFullFileName);

            (var providers, var performanceTestMessages) = GetBenchmarkMetadata(assemblyFileName);
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

            WriteToXmlFile(assemblyFileName, userFullFileName, sessionName, performanceTestMessages);
        }

        public static void ProfileNonWindows(string assemblyFileName, string sessionName, Action action, int bufferSizeMB = 128)
        {
            action.Invoke();
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

        private static void WriteXmlFile(AssemblyModel assemblyModel)
        {
            var xmlAssembliesElement = new XElement("assemblies");
            var xmlAssemblyElement = new XElement("assembly", new XAttribute("name", Path.GetFileName(assemblyModel.Name)));
            xmlAssembliesElement.Add(xmlAssemblyElement);
            var xmlCollectionElement = new XElement("collection");
            xmlAssemblyElement.Add(xmlCollectionElement);

            foreach (var testModel in assemblyModel.Collection)
            {
                var xmlTestElement = new XElement(
                    "test",
                    new XAttribute("name", testModel.Name),
                    new XAttribute("type", testModel.ClassName),
                    new XAttribute("method", testModel.Method));
                xmlCollectionElement.Add(xmlTestElement);
                var xmlPerformanceElement = new XElement("performance");
                xmlTestElement.Add(xmlPerformanceElement);
                var xmlMetricsElement = new XElement("metrics");
                xmlPerformanceElement.Add(xmlMetricsElement);

                foreach (var metric in testModel.Performance.Metrics)
                {
                    var xmlMetricElement = new XElement(
                        metric.Name,
                        new XAttribute("displayName", metric.DisplayName),
                        new XAttribute("unit", metric.Unit));
                    xmlMetricsElement.Add(xmlMetricElement);
                }

                var xmlIterationsElement = new XElement("iterations");
                xmlPerformanceElement.Add(xmlIterationsElement);

                var index = 0;
                foreach (var iterationModel in testModel.Performance.IterationModels)
                {
                    var xmlAttributes = new List<XAttribute>
                    {
                        new XAttribute("index", index++)
                    };

                    foreach (var kvp in iterationModel.Iteration)
                    {
                        xmlAttributes.Add(new XAttribute(kvp.Key, kvp.Value));
                    }

                    var xmlIterationElement = new XElement(
                        "iteration",
                        xmlAttributes.ToArray());
                    xmlIterationsElement.Add(xmlIterationElement);
                }
            }

            var xmlPath = $"{Path.GetFileNameWithoutExtension(assemblyModel.Name)}.xml";
            var xmlDoc = new XDocument(xmlAssembliesElement);
            using (var xmlFile = File.Create(xmlPath))
            {
                xmlDoc.Save(xmlFile);
            }
            WriteInfoLine($"XML BenchView tests saved to \"{xmlPath}\"");
        }

        private static void WriteToXmlFile(
            string assemblyFileName,
            string etlFileName,
            string sessionName,
            IEnumerable<PerformanceTestMessage> performanceTestMessages)
        {
            WriteXmlFile(GetAssemblyModel(
                assemblyFileName, etlFileName, sessionName, performanceTestMessages));
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

        private static (IEnumerable<ProviderInfo> providers, IEnumerable<PerformanceTestMessage> performanceTestMessages) GetBenchmarkMetadata(string assemblyFileName)
        {
            using (var controller = new XunitFrontController(
                assemblyFileName: assemblyFileName,
                shadowCopy: false,
                appDomainSupport: AppDomainSupport.Denied,
                diagnosticMessageSink: new ConsoleDiagnosticMessageSink()))
            {
                using (var testMessageVisitor = new PerformanceTestMessageVisitor())
                {
                    controller.Find(
                        includeSourceInformation: false,
                        messageSink: testMessageVisitor,
                        discoveryOptions: TestFrameworkOptions.ForDiscovery());

                    testMessageVisitor.Finished.WaitOne();

                    var providers =
                        from test in testMessageVisitor.Tests
                        from metric in test.Metrics.Cast<PerformanceMetric>()
                        from provider in metric.ProviderInfo
                        select provider;

                    providers = ProviderInfo.Merge(providers);
                    return (ProviderInfo.Merge(RequiredProviders.Concat(providers)), testMessageVisitor.Tests);
                }
            }
        }

        private static void SetPreciseMachineCounters(IEnumerable<ProviderInfo> providers)
        {
            if (IsWindows8OrGreater)
            {
                /*
                 *  TODO: Add default BranchMispredictions,CacheMisses flags (if available)
                 *    1. This reads as: flags read from config file in the case of Windows!
                 *    2. Format { "Name" : "BranchMispredictions", "interval" : "100000" }
                 */

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
                    /*
                     * FIXME: This function changes the -pmcsources intervals machine wide.
                     *  Maybe we should undo/revert these changes!
                     */
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
            return (keywords != 0) ? true : false;
        }

        private static bool IsWindows8OrGreater => IsWindows8OrGreater();

        private static IEnumerable<ProviderInfo> RequiredProviders { get; }

        [Conditional("DEBUG")]
        private static void PrintProfilingInformation(string assemblyFileName, string sessionName, string userFullFileName)
        {
            WriteDebugLine("  ===== ETW Profiling information =====");
            WriteDebugLine($"       Assembly: {assemblyFileName}");
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
