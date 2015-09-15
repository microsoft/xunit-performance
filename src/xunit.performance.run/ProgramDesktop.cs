// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing;
using Microsoft.Xunit.Performance.Sdk;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance
{
    internal class Program : ProgramCore
    {
        private static int Main(string[] args)
        {
            return new Program().Run(args);
        }

        internal override IPerformanceMetricReader GetPerformanceMetricReader(IEnumerable<PerformanceTestInfo> tests, string pathBase, string runId)
        {
            string etlPath = pathBase + ".etl";
            using (var source = new ETWTraceEventSource(etlPath))
            {
                if (source.EventsLost > 0)
                    throw new Exception($"Events were lost in trace '{etlPath}'");

                var evaluationContext = new EtwPerformanceMetricEvaluationContext(etlPath, source, tests, runId);
                try
                {
                    source.Process();

                    return evaluationContext;
                }
                catch
                {
                    evaluationContext.Dispose();
                    throw;
                }
            }
        }

        internal override IDisposable StartTracing(IEnumerable<PerformanceTestInfo> tests, string pathBase)
        {
            PrintIfVerbose($"Starting ETW tracing. Logging to {pathBase}");

            var allEtwProviders =
                from test in tests
                from metric in test.Metrics.Cast<PerformanceMetric>()
                from provider in metric.ProviderInfo
                select provider;

            var mergedEtwProviders = ProviderInfo.Merge(allEtwProviders);

            return ETWLogging.StartAsync(pathBase, mergedEtwProviders).GetAwaiter().GetResult();
        }

        internal override string GetRuntimeVersion()
        {
            return Environment.Version.ToString();
        }
    }
}
