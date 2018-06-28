using Microsoft.Xunit.Performance.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance.Api
{
    sealed class PerformanceTestMessageSink : TestMessageSink
    {
        readonly Func<bool> cancelThunk;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerformanceTestMessageSink"/> class.
        /// </summary>
        /// <param name="cancelThunk">An optional thunk which can be used to control cancellation.</param>
        public PerformanceTestMessageSink(Func<bool> cancelThunk = null)
        {
            this.cancelThunk = cancelThunk ?? (() => false);
            Tests = new List<PerformanceTestMessage>();
            Discovery.TestCaseDiscoveryMessageEvent += Discovery_TestCaseDiscoveryMessageEvent;
            Discovery.DiscoveryCompleteMessageEvent += Discovery_DiscoveryCompleteMessageEvent;
        }

        /// <summary>
        /// Gets an event which is signaled once discovery is finished.
        /// </summary>
        public ManualResetEvent Finished { get; } = new ManualResetEvent(initialState: false);

        /// <summary>
        /// The list of discovered test cases.
        /// </summary>
        public HashSet<ITestCase> TestCases { get; } = new HashSet<ITestCase>();

        public List<PerformanceTestMessage> Tests { get; }

        /// <inheritdoc/>
        public override void Dispose()
        {
            Finished.Dispose();
            base.Dispose();
        }

        public override bool OnMessageWithTypes(IMessageSinkMessage message, HashSet<string> messageTypes) => base.OnMessageWithTypes(message, messageTypes) && !cancelThunk();

        static IEnumerable<IAttributeInfo> GetMetricAttributes(ITestMethod testMethod) => testMethod.Method.GetCustomAttributes(typeof(IPerformanceMetricAttribute).AssemblyQualifiedName)
                .Concat(testMethod.TestClass.Class.GetCustomAttributes(typeof(IPerformanceMetricAttribute).AssemblyQualifiedName))
                .Concat(testMethod.TestClass.Class.Assembly.GetCustomAttributes(typeof(IPerformanceMetricAttribute).AssemblyQualifiedName));

        static IPerformanceMetricDiscoverer GetPerformanceMetricDiscoverer(IAttributeInfo metricDiscovererAttribute)
        {
            if (metricDiscovererAttribute == null)
                throw new ArgumentNullException(nameof(metricDiscovererAttribute));

            var args = metricDiscovererAttribute.GetConstructorArguments().Cast<string>().ToList();
            var discovererType = GetType(args[1], args[0]);
            return (discovererType == null) ? null : (IPerformanceMetricDiscoverer)Activator.CreateInstance(discovererType);
        }

        static Type GetType(string assemblyName, string typeName)
        {
            try
            {
                // Make sure we only use the short form
                var an = new AssemblyName(assemblyName);
                var assembly = Assembly.Load(new AssemblyName { Name = an.Name, Version = an.Version });
                return assembly.GetType(typeName);
            }
            catch
            {
            }

            return null;
        }

        void Discovery_DiscoveryCompleteMessageEvent(MessageHandlerArgs<IDiscoveryCompleteMessage> args) => Finished.Set();

        void Discovery_TestCaseDiscoveryMessageEvent(MessageHandlerArgs<ITestCaseDiscoveryMessage> args)
        {
            var testCaseDiscovered = args.Message;
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
                    TestCases.Add(args.Message.TestCase);
                    Tests.Add(new PerformanceTestMessage
                    {
                        TestCase = testCaseDiscovered.TestCase,
                        Metrics = metrics
                    });
                }
            }
        }
    }
}