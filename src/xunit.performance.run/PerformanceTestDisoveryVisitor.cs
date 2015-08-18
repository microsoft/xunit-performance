using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Xunit.Performance
{
    internal class PerformanceTestDiscoveryVisitor : global::Xunit.TestMessageVisitor<IDiscoveryCompleteMessage>
    {
        public readonly List<PerformanceTestInfo> Tests = new List<PerformanceTestInfo>();
        private XunitProjectAssembly _assembly;
        private XunitFilters _filters;
        private IMessageSink _diagnosticMessageSink;

        public PerformanceTestDiscoveryVisitor(XunitProjectAssembly assembly, XunitFilters filters, IMessageSink diagnosticMessageSink)
        {
            _assembly = assembly;
            _filters = filters;
            _diagnosticMessageSink = diagnosticMessageSink;
        }

        protected override bool Visit(ITestCaseDiscoveryMessage testCaseDiscovered)
        {
            var testCase = testCaseDiscovered.TestCase;

            if (testCase.Traits.GetOrDefault("Benchmark")?.Contains("true") ?? false &&
                string.IsNullOrEmpty(testCase.SkipReason) &&
                _filters.Filter(testCase))
            {
                //
                // Get the performance metrics for this method
                //
                var testMethod = testCaseDiscovered.TestMethod;
                List<IPerformanceMetric> metrics = new List<IPerformanceMetric>();
                foreach (var metricAttr in GetMetricAttributes(testMethod))
                {
                    var discovererAttr = metricAttr.GetCustomAttributes(typeof(PerformanceMetricDiscovererAttribute)).First();
                    var discoverer = GetPerformanceMetricDiscoverer(discovererAttr);
                    metrics.AddRange(discoverer.GetMetrics(metricAttr));
                }

                Tests.Add(new PerformanceTestInfo { Assembly = _assembly, TestCase = testCaseDiscovered.TestCase, Metrics = metrics });
            }

            return true;
        }

        private IEnumerable<IAttributeInfo> GetMetricAttributes(ITestMethod testMethod)
        {
            return testMethod.Method.GetCustomAttributes(typeof(IPerformanceMetricAttribute))
                .Concat(testMethod.TestClass.Class.GetCustomAttributes(typeof(IPerformanceMetricAttribute)))
                .Concat(testMethod.TestClass.Class.Assembly.GetCustomAttributes(typeof(IPerformanceMetricAttribute)));
        }

        private Type GetType(string assemblyName, string typeName)
        {
            //if (assemblyName.EndsWith(ExecutionHelper.SubstitutionToken, StringComparison.OrdinalIgnoreCase))
            //    assemblyName = assemblyName.Substring(0, assemblyName.Length - ExecutionHelper.SubstitutionToken.Length + 1) + ExecutionHelper.PlatformSpecificAssemblySuffix;

            Assembly assembly = null;
            try
            {
                // Make sure we only use the short form
                var an = new AssemblyName(assemblyName);
                assembly = Assembly.Load(new AssemblyName { Name = an.Name, Version = an.Version });

            }
            catch { }

            if (assembly == null)
                return null;

            return assembly.GetType(typeName);
        }

        private IPerformanceMetricDiscoverer GetPerformanceMetricDiscoverer(IAttributeInfo metricDiscovererAttribute)
        {
            var args = metricDiscovererAttribute.GetConstructorArguments().Cast<string>().ToList();
            var discovererType = GetType(args[1], args[0]);
            if (discovererType == null)
                return null;

            return ExtensibilityPointFactory.Get<IPerformanceMetricDiscoverer>(_diagnosticMessageSink, discovererType);
        }
    }
}
