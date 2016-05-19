// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Xunit.Performance.Analysis
{
    internal class HtmlResultsWriter : AbstractResultsWriter
    {
        public HtmlResultsWriter(Dictionary<string, string> allMetrics,
                            Dictionary<string, Dictionary<string, TestResult>> testResults,
                            Dictionary<string, List<TestResultComparison>> comparisonResults,
                            string htmlOutputPath)
            : base(allMetrics, testResults, comparisonResults, htmlOutputPath)
        {
        }

        //--//

        protected override void WriteStatistics()
        {
            //
            // Write comparison results
            //
            foreach (var metricName in ComparisonResults.Keys)
            {
                var comparisonResults = ComparisonResults[metricName];

                this.OutputStream.WriteLine($"<h1>Metric: {metricName}</h1>");

                foreach (var comparison in comparisonResults.GroupBy(r => $"Comparison: {r.ComparisonResult.RunId} | Baseline: {r.BaselineResult.RunId}"))
                {
                    this.OutputStream.WriteLine($"<h2>{comparison.Key}</h2>");
                    this.OutputStream.WriteLine("<table>");

                    this.OutputStream.WriteLine($"<tr><td><b>Test name</b></td><td><b>Change</b></td><td><b>Error</b></td></tr>");

                    foreach (var test in from c in comparison orderby c.SortChange descending select c)
                    {
                        var passed = test.Passed;

                        string color;
                        if (!passed.HasValue)
                            color = "black";
                        else if (passed.Value)
                            color = "green";
                        else
                            color = "red";

                        this.OutputStream.WriteLine($"<tr><td>{test.TestName}</td><td><font  color={color}>{test.PercentChange.ToString("+##.#%;-##.#%")}</font></td><td>+/-{test.PercentChangeError.ToString("P1")}</td></tr>");
                    }
                    this.OutputStream.WriteLine("</table>");
                }
            }

            this.OutputStream.WriteLine("<hr>");
        }

        protected override void WriteHeader()
        {
            this.OutputStream.WriteLine("<html><body>");

            //
            // List metrics
            //

            //
            // A short header to list what metrics have been targeted
            //
            this.OutputStream.WriteLine($"<h1>Targeted metrics: </h1>");

            foreach (var metricName in ComparisonResults.Keys)
            {
                var comparisonResults = ComparisonResults[metricName];

                this.OutputStream.WriteLine($"<p><b>{metricName}</b>: {comparisonResults.Count} data points</p>");
            }

            this.OutputStream.WriteLine("<hr>");
        }

        protected override void WriteFooter()
        {
            this.OutputStream.WriteLine("</html></body>");
        }

        protected override void WriteIndividualResults()
        {
            //
            // Write Individual test results
            // 

            foreach (var run in TestResults)
            {
                this.OutputStream.WriteLine($"<h1>Individual results: {run.Key}</h1>");

                foreach (var metricName in AllMetrics.Keys)
                {
                    this.OutputStream.WriteLine($"<h2>Metric: {metricName}</h2>");

                    this.OutputStream.WriteLine($"<table>");
                    this.OutputStream.WriteLine($"<tr><th>Test</th><th>Unit</th><th>Min</th><th>Mean</th><th>Max</th><th>Margin</th><th>StdDev</th></tr>");


                    foreach (var test in run.Value)
                    {
                        if (test.Value.Stats.ContainsKey(metricName))
                        {
                            var stats = test.Value.Stats[metricName].RunningStatistics;
                            this.OutputStream.WriteLine($"<tr><td>{test.Value.TestName}</td><td>{AllMetrics[metricName]}</td><td>{stats.Minimum.ToString("G3")}</td><td>{stats.Mean.ToString("G3")}</td><td>{stats.Maximum.ToString("G3")}</td><td>{stats.MarginOfError(Properties.ErrorConfidence).ToString("P1")}</td><td>{stats.StandardDeviation.ToString("G3")}</td></tr>");
                        }
                    }
                    this.OutputStream.WriteLine($"</table>");
                }
            }
        }
    }
}
