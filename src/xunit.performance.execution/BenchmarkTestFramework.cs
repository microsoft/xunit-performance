// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Xunit.Performance
{
    internal class BenchmarkTestFrameworkTypeDiscoverer : ITestFrameworkTypeDiscoverer
    {
        public Type GetTestFrameworkType(IAttributeInfo attribute)
        {
            if (BenchmarkConfiguration.RunningAsPerfTest)
                return typeof(BenchmarkTestFramework);
            else
                return typeof(XunitTestFramework);
        }
    }

    internal class BenchmarkTestFramework : XunitTestFramework
    {
        public BenchmarkTestFramework(IMessageSink messageSink) : base(messageSink) { }

        protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName)
        {
            return new BenchmarkTestFrameworkExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);
        }
    }

    internal class BenchmarkTestFrameworkExecutor : XunitTestFrameworkExecutor
    {
        public BenchmarkTestFrameworkExecutor(AssemblyName assemblyName, ISourceInformationProvider sourceInformationProvider, IMessageSink diagnosticMessageSink)
            : base(assemblyName, sourceInformationProvider, diagnosticMessageSink)
        {
        }

        protected override void RunTestCases(IEnumerable<IXunitTestCase> testCases, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
        {
            executionOptions.SetValue("xunit.execution.SynchronousMessageReporting", (bool?)true);
            executionOptions.SetValue("xunit.execution.DisableParallelization", (bool?)true);
            base.RunTestCases(testCases, executionMessageSink, executionOptions);
        }
    }
}
