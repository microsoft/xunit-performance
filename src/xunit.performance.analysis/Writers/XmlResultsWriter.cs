// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Xml.Linq;
#if !LINUX_BUILD
using MathNet.Numerics.Statistics;
#endif

namespace Microsoft.Xunit.Performance.Analysis
{
    internal class XmlResultsWriter : AbstractResultsWriter
    {
        public XmlResultsWriter(
            Dictionary<string, MetricInfo> allMetrics,
            Dictionary<string, Dictionary<string, TestResult>> testResults,
            Dictionary<string, List<TestResultComparison>> comparisonResults,
            string outputPath) : base(allMetrics, testResults, comparisonResults, outputPath)
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
            var resultElem = new XElement("results");
            var xmlDoc = new XDocument(resultElem);

            foreach (var run in TestResults)
            {
                var runIdElem = new XElement("run", new XAttribute("id", run.Key));
                resultElem.Add(runIdElem);

                foreach (var result in run.Value.Values)
                {
                    var testElem = new XElement("test", new XAttribute("name", result.TestName));
                    runIdElem.Add(testElem);

                    var summaryElem = new XElement("summary");
                    testElem.Add(summaryElem);

                    foreach (var stat in result.Stats)
                    {
                        RunningStatistics runningStats = stat.Value.RunningStatistics;
                        summaryElem.Add(new XElement(stat.Key,
                            new XAttribute("min", runningStats.Minimum.ToString("G3")),
                            new XAttribute("mean", runningStats.Mean.ToString("G3")),
                            new XAttribute("max", runningStats.Maximum.ToString("G3")),
                            new XAttribute("marginOfError", runningStats.MarginOfError(Properties.ErrorConfidence).ToString("G3")),
                            new XAttribute("stddev", runningStats.StandardDeviation.ToString("G3"))));
                    }
                }
            }

            //
            // Write comparison results
            //
            foreach (var metricName in ComparisonResults.Keys)
            {
                var comparisonResults = ComparisonResults[metricName];

                foreach (var comparison in comparisonResults)
                {
                    var comparisonElem = new XElement("comparison", new XAttribute("test", comparison.TestName), new XAttribute("baselineId", comparison.BaselineResult.RunId), new XAttribute("comparisonId", comparison.ComparisonResult.RunId));
                    resultElem.Add(comparisonElem);

                    comparisonElem.Add(
                        new XElement(metricName,
                        new XAttribute("changeRatio", comparison.PercentChange.ToString("G3")),
                        new XAttribute("changeRatioError", comparison.PercentChangeError.ToString("G3"))));
                }
            }

            xmlDoc.Save(this.OutputStream);
        }
    }
}
