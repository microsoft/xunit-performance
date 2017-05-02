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
        }

        private class PerformanceMetricComparer : IEqualityComparer<PerformanceMetricInfo>
        {
            public bool Equals(PerformanceMetricInfo x, PerformanceMetricInfo y)
            {
                return x.GetType().Equals(y.GetType());
            }

            public int GetHashCode(PerformanceMetricInfo obj)
            {
                return obj.GetType().GetHashCode();
            }
        }

        public static XUnitPerformanceMetricData GetMetadata(
            string assemblyFileName,
            IEnumerable<PerformanceMetric> performanceMetricInfos,
            bool collectDefaultMetrics)
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

                    var testProviders =
                        from test in testMessageVisitor.Tests
                        from metric in test.Metrics.Cast<PerformanceMetric>()
                        from provider in metric.ProviderInfo
                        select provider;
                    testProviders = ProviderInfo.Merge(testProviders);

                    var userProviders = Enumerable.Empty<ProviderInfo>();
                    foreach (var performanceMetricInfo in performanceMetricInfos)
                        userProviders = ProviderInfo.Merge(userProviders.Concat(performanceMetricInfo.ProviderInfo));

                    var comparer = new PerformanceMetricComparer();

                    // Inject performance metrics into the tests
                    foreach (var test in testMessageVisitor.Tests)
                    {
                        test.Metrics = collectDefaultMetrics ?
                            test.Metrics.Union(performanceMetricInfos, comparer) :
                            performanceMetricInfos;
                    }

                    return new XUnitPerformanceMetricData {
                        Providers = ProviderInfo.Merge(RequiredProviders.Concat(testProviders).Concat(userProviders)),
                        PerformanceTestMessages = testMessageVisitor.Tests
                    };
                }
            }
        }

        public static XUnitPerformanceMetricData GetMetadata(string assemblyFileName)
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

                    var testProviders =
                        from test in testMessageVisitor.Tests
                        from metric in test.Metrics.Cast<PerformanceMetric>()
                        from provider in metric.ProviderInfo
                        select provider;
                    testProviders = ProviderInfo.Merge(testProviders);

                    return new XUnitPerformanceMetricData {
                        Providers = ProviderInfo.Merge(RequiredProviders.Concat(testProviders)),
                        PerformanceTestMessages = testMessageVisitor.Tests
                    };
                }
            }
        }

        /// <summary>
        /// Defines the default list of providers needed to record ETW.
        /// </summary>
        public static List<ProviderInfo> RequiredProviders { get; }
    }
}
