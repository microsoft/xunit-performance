// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Xunit.Performance.Analysis
{
    internal class CsvResultsWriter : CsvStatsWriter
    {
        private string MetricName;
        public CsvResultsWriter(Dictionary<string, MetricInfo> allMetrics,
                            Dictionary<string, Dictionary<string, TestResult>> testResults,
                            Dictionary<string, List<TestResultComparison>> comparisonResults,
                            string outputPath, string metricName)
            : base(allMetrics, testResults, comparisonResults, Path.Combine(outputPath, metricName+".csv"))
        {
            this.MetricName = metricName;
        }

        //--//

        protected override void WriteStatistics()
        {
        }

        protected override void WriteFooter()
        {
        }

        protected override void WriteHeader()
        {
        }

        protected override void WriteIndividualResults()
        {
            foreach (var run in TestResults)
            {
                foreach (var result in run.Value.Values)
                {
                    foreach (var iteration in result.Iterations)
                    {
                        // If the current iteration does not contain this metric, skip
                        if (!iteration.MetricValues.ContainsKey(this.MetricName))
                            continue;

                        this.OutputStream.WriteLine(
                            String.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\"",
                                            EscapeCsvString(iteration.RunId),
                                            EscapeCsvString(iteration.RunId),
                                            EscapeCsvString(iteration.TestName),
                                            iteration.MetricValues[this.MetricName].ToString()));

                    }

                }
            }
        }

    }
}
