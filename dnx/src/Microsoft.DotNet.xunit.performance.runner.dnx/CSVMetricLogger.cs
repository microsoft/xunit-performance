// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance.Sdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance
{
    internal class CSVMetricLogger : IPerformanceMetricLogger
    {
        private readonly string _csvPath;

        public CSVMetricLogger(XunitPerformanceProject project)
        {
            _csvPath = Path.GetFullPath(Path.Combine(project.OutputDir, project.RunId + ".csv"));

            var diagnosticMessageSink = new ConsoleReporter();

            foreach (var assembly in project.Assemblies)
            {
                Console.WriteLine($"Discovering tests for {assembly.AssemblyFilename}.");

                // Note: We do not use shadowCopy because that creates a new AppDomain which can cause
                // assembly load failures with delay-signed or "fake signed" assemblies.
                using (var controller = new XunitFrontController(
                    assemblyFileName: assembly.AssemblyFilename,
                    shadowCopy: true,
                    appDomainSupport: AppDomainSupport.Denied,
                    diagnosticMessageSink: new ConsoleDiagnosticsMessageVisitor())
                )
                using (var discoveryVisitor = new PerformanceTestDiscoveryVisitor(assembly, project.Filters, diagnosticMessageSink))
                {
                    controller.Find(includeSourceInformation: false, messageSink: discoveryVisitor, discoveryOptions: TestFrameworkOptions.ForDiscovery());
                    discoveryVisitor.Finished.WaitOne();
                    foreach (PerformanceTestInfo info in discoveryVisitor.Tests)
                        project.Filters.IncludedMethods.Add(info.TestCase.TestMethod.Method.Name);
                }
            }
        }

        public IDisposable StartLogging()
        {
            Environment.SetEnvironmentVariable("XUNIT_PERFORMANCE_FILE_LOG_PATH", _csvPath);
            return null;
        }

        public IPerformanceMetricReader GetReader()
        {
            return new CSVMetricReader(_csvPath);
        }

        private class ConsoleReporter : IMessageSink
        {
            public bool OnMessage(IMessageSinkMessage message)
            {
                Console.WriteLine(message.ToString());
                return true;
            }
        }

        internal class ConsoleDiagnosticsMessageVisitor : TestMessageVisitor<IDiagnosticMessage>
        {
            protected override bool Visit(IDiagnosticMessage diagnosticMessage)
            {
                Console.Error.WriteLine(diagnosticMessage.Message);
                return true;
            }
        }
    }
}
