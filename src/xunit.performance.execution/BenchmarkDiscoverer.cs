// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Xunit.Performance
{
    class BenchmarkDiscoverer : TheoryDiscoverer, ITraitDiscoverer
    {
        readonly IMessageSink _diagnosticMessageSink;

        public BenchmarkDiscoverer(IMessageSink diagnosticMessageSink)
            : base(diagnosticMessageSink) => _diagnosticMessageSink = diagnosticMessageSink;

        public override IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo benchmarkAttribute)
        {
            var defaultMethodDisplay = discoveryOptions.MethodDisplayOrDefault();

            //
            // Special case Skip, because we want a single Skip (not one per data item), and a skipped test may
            // not actually have any data (which is quasi-legal, since it's skipped).
            //
            if (benchmarkAttribute.GetNamedArgument<string>("Skip") != null)
            {
                yield return new XunitTestCase(_diagnosticMessageSink, defaultMethodDisplay, testMethod);
                yield break;
            }

            //
            // Use the TheoryDiscoverer to enumerate the cases.  We can't do this, because
            // xUnit doesn't expose everything we need (for example, the ability to ask if an
            // object is xUnit-serializable).
            //
            foreach (var theoryCase in base.Discover(discoveryOptions, testMethod, benchmarkAttribute))
            {
                if (theoryCase is XunitTheoryTestCase)
                {
                    //
                    // TheoryDiscoverer returns one of these if it cannot enumerate the cases now.
                    // We'll return a BenchmarkTestCase with no data associated.
                    //
                    yield return new BenchmarkTestCase(_diagnosticMessageSink, defaultMethodDisplay, testMethod, benchmarkAttribute);
                }
                else
                {
                    //
                    // This is a test case with data
                    //
                    yield return new BenchmarkTestCase(_diagnosticMessageSink, defaultMethodDisplay, testMethod, benchmarkAttribute, theoryCase.TestMethodArguments);
                }
            }
        }

        public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute) => new[] { new KeyValuePair<string, string>("Benchmark", "true") };
    }
}