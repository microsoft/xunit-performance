using System.Collections.Generic;
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

            if (factAttribute.GetNamedArgument<string>("Skip") != null)
                return new[] { new XunitTestCase(_diagnosticMessageSink, defaultMethodDisplay, testMethod) };

            return new XunitTestCase[] { new BenchmarkTestCase(_diagnosticMessageSink, defaultMethodDisplay, testMethod, factAttribute) };
        }
    }
}
