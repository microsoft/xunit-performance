using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.Xunit.Performance.Api
{
    internal static class XunitBenchmark
    {
        static XunitBenchmark()
        {
            // TODO: Read this from file.
            //   1. This reads as: flags read from config file based on running platform!
            //   2. Format could be something like this: `{ "Name" : "BranchMispredictions", "interval" : "100000" }`
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
                    return (ProviderInfo.Merge(RequiredProviders.Concat(providers)), testMessageVisitor.Tests);
                }
            }
        }

        public static IEnumerable<ProviderInfo> RequiredProviders { get; }
    }
}
