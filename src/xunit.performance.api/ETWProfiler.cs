using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Xunit.Performance.Sdk;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;
using Xunit.Abstractions;
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
                new UserProviderInfo()
                {
                    ProviderGuid = Guid.Parse("A3B447A8-6549-4158-9BAD-76D442A47061"),
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
        }

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
        public static void Profile(string assemblyFileName, string sessionName, Action action, int bufferSizeMB = 128)
        {
            var sessionFileName = $"{sessionName}-{Path.GetFileNameWithoutExtension(assemblyFileName)}";
            var userFileName = $"{sessionFileName}.etl";
            var kernelFileName = $"{sessionFileName}.kernel.etl";
            var currentWorkingDirectory = Directory.GetCurrentDirectory();
            var userFullFileName = Path.Combine(currentWorkingDirectory, userFileName);
            var kernelFullFileName = Path.Combine(currentWorkingDirectory, kernelFileName);

            PrintProfilingInformation(assemblyFileName, sessionName, userFullFileName);

            var providers = GetProviders(assemblyFileName);
            var kernelProviderInfo = providers.OfType<KernelProviderInfo>().FirstOrDefault();
            SetPreciseMachineCounters(providers);

            var needKernelSession = NeedSeparateKernelSession(kernelProviderInfo);
            using (var kernelSession = needKernelSession ? new TraceEventSession(KernelTraceEventParser.KernelSessionName, kernelFullFileName) : null)
            {
                if (kernelSession != null)
                {
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
        }

        private static IEnumerable<ProviderInfo> GetProviders(string assemblyFileName)
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
                    return ProviderInfo.Merge(RequiredProviders.Concat(providers));
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
                    TraceEventProfileSources.Set(profileSourceIDs.ToArray(), profileSourceIntervals.ToArray());
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
            if (((KernelTraceEventParser.Keywords)kernelProviderInfo.Keywords & KernelTraceEventParser.Keywords.PMCProfile) != 0)
                return true;

            return false;
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

        private sealed class PerformanceTestMessage
        {
            public ITestCase TestCase;
            public IEnumerable<PerformanceMetricInfo> Metrics;
        }

        private sealed class PerformanceTestMessageVisitor : TestMessageVisitor<IDiscoveryCompleteMessage>
        {
            public PerformanceTestMessageVisitor()
            {
                Tests = new List<PerformanceTestMessage>();
            }

            public List<PerformanceTestMessage> Tests { get; }

            protected override bool Visit(ITestCaseDiscoveryMessage testCaseDiscovered)
            {
                var testCase = testCaseDiscovered.TestCase;
                if (string.IsNullOrEmpty(testCase.SkipReason)) /* TODO: Currently there are not filters */
                {
                    var testMethod = testCaseDiscovered.TestMethod;
                    var metrics = new List<PerformanceMetricInfo>();
                    var attributesInfo = GetMetricAttributes(testMethod);

                    foreach (var attributeInfo in attributesInfo)
                    {
                        var assemblyQualifiedAttributeTypeName = typeof(PerformanceMetricDiscovererAttribute).AssemblyQualifiedName;
                        var discovererAttr = attributeInfo.GetCustomAttributes(assemblyQualifiedAttributeTypeName).FirstOrDefault();
                        var discoverer = GetPerformanceMetricDiscoverer(discovererAttr);
                        metrics.AddRange(discoverer.GetMetrics(attributeInfo));
                    }

                    if (metrics.Count > 0)
                    {
                        Tests.Add(new PerformanceTestMessage
                        {
                            TestCase = testCaseDiscovered.TestCase,
                            Metrics = metrics
                        });
                    }
                }
                return true;
            }

            private static IEnumerable<IAttributeInfo> GetMetricAttributes(ITestMethod testMethod)
            {
                return testMethod.Method.GetCustomAttributes(typeof(IPerformanceMetricAttribute).AssemblyQualifiedName)
                    .Concat(testMethod.TestClass.Class.GetCustomAttributes(typeof(IPerformanceMetricAttribute).AssemblyQualifiedName))
                    .Concat(testMethod.TestClass.Class.Assembly.GetCustomAttributes(typeof(IPerformanceMetricAttribute).AssemblyQualifiedName));
            }

            private static IPerformanceMetricDiscoverer GetPerformanceMetricDiscoverer(IAttributeInfo metricDiscovererAttribute)
            {
                if (metricDiscovererAttribute == null)
                    throw new ArgumentNullException();

                var args = metricDiscovererAttribute.GetConstructorArguments().Cast<string>().ToList();
                var discovererType = GetType(args[1], args[0]);
                if (discovererType == null)
                    return null;

                return (IPerformanceMetricDiscoverer)Activator.CreateInstance(discovererType);
            }

            private static Type GetType(string assemblyName, string typeName)
            {
                try
                {
                    // Make sure we only use the short form
                    var an = new AssemblyName(assemblyName);
                    Assembly assembly = Assembly.Load(new AssemblyName { Name = an.Name, Version = an.Version });
                    return assembly.GetType(typeName);
                }
                catch
                {
                }

                return null;
            }
        }
    }
}
