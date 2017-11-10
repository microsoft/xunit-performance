using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.Xunit.Performance.Api
{
    internal static partial class XunitBenchmark
    {
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

                    // Inject performance metrics into the tests
                    foreach (var test in testMessageVisitor.Tests)
                    {
                        test.Metrics = collectDefaultMetrics ?
                            test.Metrics.Union(performanceMetricInfos, new PerformanceMetricInfoComparer()) :
                            performanceMetricInfos;
                    }

                    return new XUnitPerformanceMetricData {
                        Providers = ProviderInfo.Merge(testProviders.Concat(userProviders)),
                        PerformanceTestMessages = testMessageVisitor.Tests,
                    };
                }
            }
        }
    }
}
