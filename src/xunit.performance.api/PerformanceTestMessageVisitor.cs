using Microsoft.Xunit.Performance.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance.Api
{
    internal sealed class PerformanceTestMessageVisitor : TestMessageVisitor<IDiscoveryCompleteMessage>
    {
        public PerformanceTestMessageVisitor()
        {
            Tests = new List<PerformanceTestMessage>();
        }

        public List<PerformanceTestMessage> Tests { get; }

        protected override bool Visit(ITestCaseDiscoveryMessage testCaseDiscovered)
        {
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
                    Tests.Add(new PerformanceTestMessage
                    {
                        TestCase = testCaseDiscovered.TestCase,
                        Metrics = metrics
                    });
                }
            }
            return true;
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
    }
}