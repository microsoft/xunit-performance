using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Xunit.Performance.Execution;
using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using static Microsoft.Xunit.Performance.Api.PerformanceLogger;

namespace Microsoft.Xunit.Performance.Api
{
    internal static class XunitBenchmark
    {
        static XunitBenchmark()
        {
            // TODO: Read this from file.
            //   1. This reads as: flags read from config file based on running platform!
            //   2. Format could be something like this: `{ "Name" : "BranchMispredictions", "interval" : "100000" }`
            PerformanceMonitorCounters = new List<IPerformanceMonitorCounter>
            {
                new BranchMispredictionsPerformanceMonitorCounter(),
                new CacheMissesPerformanceMonitorCounter(),
            };

            PerformanceMetricInfos = new List<BasePerformanceMonitorCounter>
            {
                new GenericPerformanceMonitorCounterMetric<BranchMispredictionsPerformanceMonitorCounter>(new BranchMispredictionsPerformanceMonitorCounter()),
                new GenericPerformanceMonitorCounterMetric<CacheMissesPerformanceMonitorCounter>(new CacheMissesPerformanceMonitorCounter()),
            };

            RequiredProviders = new List<ProviderInfo>
            {
                new KernelProviderInfo()
                {
                    Keywords = (ulong)KernelTraceEventParser.Keywords.Process | (ulong)KernelTraceEventParser.Keywords.Profile,
                    StackKeywords = (ulong)KernelTraceEventParser.Keywords.Profile
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
                        (ulong)ClrTraceEventParser.Keywords.Jit |
                        (ulong)ClrTraceEventParser.Keywords.JittedMethodILToNativeMap |
                        (ulong)ClrTraceEventParser.Keywords.Loader |
                        (ulong)ClrTraceEventParser.Keywords.Exception |
                        (ulong)ClrTraceEventParser.Keywords.GC
                    ),
                }
            };

            foreach (var pmc in PerformanceMonitorCounters)
            {
                RequiredProviders.Add(new CpuCounterInfo()
                {
                    CounterName = pmc.Name,
                    Interval = pmc.Interval,
                });
            }
        }

        public static (IEnumerable<ProviderInfo> providers, IEnumerable<PerformanceTestMessage> performanceTestMessages) GetMetadata(string assemblyFileName)
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

                    // Create the list of default metrics
                    IEnumerable<PerformanceMetricInfo> defaultMetricInfos =
                        from pmcMetricInfo in PerformanceMetricInfos
                        where pmcMetricInfo.IsValidPmc
                        select pmcMetricInfo;

                    if (AllocatedBytesForCurrentThread.IsAvailable)
                        defaultMetricInfos = defaultMetricInfos.Concat(new[] { new GCAllocatedBytesForCurrentThreadMetric() });
                    else
                    {
                        WriteWarningLine(AllocatedBytesForCurrentThread.NoAvailabilityReason);
                        WriteWarningLine($"The '{GCAllocatedBytesForCurrentThreadMetric.GetAllocatedBytesForCurrentThreadDisplayName}' metric will not be collected.");
                    }

                    // Inject implicit pmc counters
                    foreach (var test in testMessageVisitor.Tests)
                        test.Metrics = test.Metrics.Concat(defaultMetricInfos);

                    return (ProviderInfo.Merge(RequiredProviders.Concat(providers)), testMessageVisitor.Tests);
                }
            }
        }

        public static List<ProviderInfo> RequiredProviders { get; }

        private static List<IPerformanceMonitorCounter> PerformanceMonitorCounters { get; }

        private static List<BasePerformanceMonitorCounter> PerformanceMetricInfos { get; }
    }
}
