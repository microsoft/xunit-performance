using System;
using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Xunit.Performance
{
    internal class BenchmarkDiscoverer : IXunitTestCaseDiscoverer
    {
        private readonly IMessageSink _diagnosticMessageSink;

        public BenchmarkDiscoverer(IMessageSink diagnosticMessageSink)
        {
            _diagnosticMessageSink = diagnosticMessageSink;
        }

        public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            var defaultMethodDisplay = discoveryOptions.MethodDisplayOrDefault();

            // Special case Skip, because we want a single Skip (not one per data item), and a skipped test may
            // not actually have any data (which is quasi-legal, since it's skipped).
            if (factAttribute.GetNamedArgument<string>("Skip") != null)
                return new[] { new XunitTestCase(_diagnosticMessageSink, defaultMethodDisplay, testMethod) };

            var dataAttributes = testMethod.Method.GetCustomAttributes(typeof(DataAttribute));

            if (discoveryOptions.PreEnumerateTheoriesOrDefault())
            {
                try
                {
                    var results = new List<XunitTestCase>();

                    foreach (var dataAttribute in dataAttributes)
                    {
                        var discovererAttribute = dataAttribute.GetCustomAttributes(typeof(DataDiscovererAttribute)).First();
                        var discoverer = ExtensibilityPointFactory.GetDataDiscoverer(_diagnosticMessageSink, discovererAttribute);
                        if (discoverer.SupportsDiscoveryEnumeration(dataAttribute, testMethod.Method))
                        {
                            foreach (var dataRow in discoverer.GetData(dataAttribute, testMethod.Method))
                            {
                                var testCase = new BenchmarkTestCase(_diagnosticMessageSink, defaultMethodDisplay, testMethod, factAttribute, dataRow);
                                results.Add(testCase);
                            }
                        }
                    }

                    if (results.Count > 0)
                        return results;
                }
                catch { }  // If something goes wrong, fall through to return just the XunitTestCase
            }

            return new XunitTestCase[] { new BenchmarkTestCase(_diagnosticMessageSink, defaultMethodDisplay, testMethod, factAttribute) };
        }
    }
}
