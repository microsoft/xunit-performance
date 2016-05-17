// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Analysis
{
    internal class CsvResultsWriter : CsvStatsWriter
    {
        public CsvResultsWriter(Dictionary<string, string> allMetrics,
                            Dictionary<string, Dictionary<string, TestResult>> testResults,
                            Dictionary<string, List<TestResultComparison>> comparisonResults,
                            string outputPath)
            : base(allMetrics, testResults, comparisonResults, outputPath)
        {
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
                        this.OutputStream.WriteLine(
                            String.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\"",
                                            EscapeCsvString(iteration.RunId),
                                            EscapeCsvString(iteration.RunId),
                                            EscapeCsvString(iteration.TestName),
                                            iteration.MetricValues[Properties.DurationMetricName].ToString()));
                    }

                }
            }
        }
    }
}
