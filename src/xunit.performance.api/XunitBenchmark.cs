using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.Xunit.Performance.Api
{
    static partial class XunitBenchmark
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
                using (var testMessageSink = new PerformanceTestMessageSink())
                {
                    controller.Find(
                        includeSourceInformation: false,
                        messageSink: testMessageSink,
                        discoveryOptions: TestFrameworkOptions.ForDiscovery());
                    testMessageSink.Finished.WaitOne();

                    var testProviders =
                        from test in testMessageSink.Tests
                        from metric in test.Metrics.Cast<PerformanceMetric>()
                        from provider in metric.ProviderInfo
                        select provider;
                    testProviders = ProviderInfo.Merge(testProviders);

                    var userProviders = Enumerable.Empty<ProviderInfo>();
                    foreach (var performanceMetricInfo in performanceMetricInfos)
                        userProviders = ProviderInfo.Merge(userProviders.Concat(performanceMetricInfo.ProviderInfo));

                    // Inject performance metrics into the tests
                    foreach (var test in testMessageSink.Tests)
                    {
                        test.Metrics = collectDefaultMetrics ?
                            test.Metrics.Union(performanceMetricInfos, new PerformanceMetricInfoComparer()) :
                            performanceMetricInfos;
                    }

                    return new XUnitPerformanceMetricData
                    {
                        Providers = ProviderInfo.Merge(testProviders.Concat(userProviders)),
                        PerformanceTestMessages = testMessageSink.Tests,
                    };
                }
            }
        }
    }
}