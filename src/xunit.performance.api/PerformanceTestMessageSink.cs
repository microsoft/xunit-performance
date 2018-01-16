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
    internal sealed class PerformanceTestMessageSink : TestMessageSink
    {
        private readonly ManualResetEvent _finished = new ManualResetEvent(false);

        public PerformanceTestMessageSink()
        {
            Tests = new List<PerformanceTestMessage>();
            Discovery.TestCaseDiscoveryMessageEvent += OnTestCaseDiscoveryMessageEvent;
            Discovery.DiscoveryCompleteMessageEvent += OnDiscoveryCompleteMessageEvent;
        }

        public List<PerformanceTestMessage> Tests { get; }

        private void OnTestCaseDiscoveryMessageEvent(MessageHandlerArgs<ITestCaseDiscoveryMessage> args)
        {
            var testCase = args.Message.TestCase;
            if (string.IsNullOrEmpty(testCase.SkipReason)) /* TODO: Currently there are not filters */
            {
                var testMethod = args.Message.TestMethod;
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
                        TestCase = args.Message.TestCase,
                        Metrics = metrics
                    });
                }
            }
        }

        private void OnDiscoveryCompleteMessageEvent(MessageHandlerArgs<IDiscoveryCompleteMessage> args)
        {
            _finished.Set();
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
                throw new ArgumentNullException(nameof(metricDiscovererAttribute));

            var args = metricDiscovererAttribute.GetConstructorArguments().Cast<string>().ToList();
            var discovererType = GetType(args[1], args[0]);
            return (discovererType == null) ? null : (IPerformanceMetricDiscoverer)Activator.CreateInstance(discovererType);
        }

        private static Type GetType(string assemblyName, string typeName)
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

        public override void Dispose()
        {
            Discovery.DiscoveryCompleteMessageEvent -= OnDiscoveryCompleteMessageEvent;
            Discovery.TestCaseDiscoveryMessageEvent -= OnTestCaseDiscoveryMessageEvent;

            base.Dispose();
        }

        public ManualResetEvent Finished => _finished;
    }
}