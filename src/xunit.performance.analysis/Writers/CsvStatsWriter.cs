// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;

namespace Microsoft.Xunit.Performance.Analysis
{
    internal class CsvStatsWriter : AbstractResultsWriter
    {
        public CsvStatsWriter(Dictionary<string, string> allMetrics,
                                Dictionary<string, Dictionary<string, TestResult>> testResults,
                                Dictionary<string, List<TestResultComparison>> comparisonResults,
                                string outputPath)
            : base(allMetrics, testResults, comparisonResults, outputPath)
        {
        }

        //--//

        protected override void WriteStatistics()
        {
            foreach (var run in TestResults)
            {
                foreach (var result in run.Value.Values)
                {
                    var durationStats = result.Stats[Properties.DurationMetricName].RunningStatistics;

                    this.OutputStream.WriteLine(
                        "\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\"",
                        EscapeCsvString(result.TestName),
                        durationStats.Count,
                        durationStats.Minimum,
                        durationStats.Maximum,
                        durationStats.Mean,
                        durationStats.StandardDeviation,
                        EscapeCsvString(GetMetricsString(result.Stats.Keys))
                        );
                }
            }
        }

        protected override void WriteFooter()
        {
        }

        protected override void WriteHeader()
        {
            this.OutputStream.WriteLine("Test, Iterations, Duration Min, Duration Max, Duration Average, Duration Stdev, Metrics");
        }

        protected override void WriteIndividualResults()
        {
        }

        protected static string EscapeCsvString(string str)
        {
            str.Trim('\"');
            // Escape the csv string
            if (str.Contains("\""))
            {
                str = str.Replace("\"", "\"\"");
            }

            if (str.Contains(","))
            {
                str = "\"\"" + str + "\"\"";
            }
            return str;
        }

        protected static string GetMetricsString(Dictionary<string, TestStatistics>.KeyCollection metrics)
        {
            StringBuilder builder = new StringBuilder();
            foreach (string metric in metrics)
            {
                builder.AppendFormat("{0};", metric);
            }

            return builder.ToString();
        }
    }
}

