// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !LINUX_BUILD
using MathNet.Numerics.Statistics;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
#if !LINUX_BUILD
using System.Runtime.Serialization;
#endif

namespace Microsoft.Xunit.Performance.Analysis
{
    internal class Program
    {
        private const double ErrorConfidence = 0.95; // TODO: make configurable

        /// <summary>
        /// The name of the Duration metric, as provided by the XML.
        /// </summary>
        private const string DurationMetricName = "Duration";

        private static int Usage()
        {
            Console.Error.WriteLine(
                "usage: xunit.performance.analysis <xmlPaths> [-compare \"baselineRunId\" \"comparisonRunId\"]  [-xml <output.xml>] [-html <output.html>]");
            return 1;
        }

        private static int Main(string[] args)
        {
            var xmlPaths = new List<string>();
            var allComparisonIds = new List<Tuple<string, string>>();
            var xmlOutputPath = (string)null;
            var htmlOutputPath = (string)null;
            var csvOutputPath = (string)null;
            var statsCsvOutputPath = (string)null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-"))
                {
                    string switchName = args[i].Substring(1).ToLowerInvariant();
                    switch (switchName)
                    {
                        case "compare":
                            if (++i >= args.Length)
                                return Usage();
                            string baseline = args[i];
                            if (++i >= args.Length)
                                return Usage();
                            string comparison = args[i];
                            allComparisonIds.Add(Tuple.Create(baseline, comparison));
                            break;

                        case "xml":
                            if (++i >= args.Length)
                                return Usage();
                            xmlOutputPath = args[i];
                            break;

                        case "html":
                            if (++i >= args.Length)
                                return Usage();
                            htmlOutputPath = args[i];
                            break;

                        case "csv":
                            if (++i >= args.Length)
                                return Usage();
                            csvOutputPath = args[i];
                            break;
                        case "stats":
                            if (++i >= args.Length)
                                return Usage();
                            statsCsvOutputPath = args[i];
                            break;

                        default:
                            return Usage();
                    }
                }
                else
                {
                    bool foundFile = false;
                    foreach (var file in ExpandFilePath(args[i]))
                    {
                        if (file.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                        {
                            foundFile = true;
                            xmlPaths.Add(file);
                        }
                        else
                        {
                            Console.Error.WriteLine($"{file}' is not a .xml file.");
                            return 1;
                        }
                    }
                    if (!foundFile)
                    {
                        Console.Error.WriteLine($"The path '{args[i]}' could not be found.");
                        return 1;
                    }
                }
            }

            if (xmlPaths.Count == 0)
                return Usage();

            var allIterations = ParseXmlFiles(xmlPaths);

            var testResults = SummarizeTestResults(allIterations);
#if !LINUX_BUILD
            var comparisonResults = DoComparisons(allComparisonIds, testResults);

            if (xmlOutputPath != null)
                WriteTestResultsXml(testResults, comparisonResults).Save(xmlOutputPath);

            if (htmlOutputPath != null)
                WriteTestResultsHtml(testResults, comparisonResults, htmlOutputPath);

            if (statsCsvOutputPath != null)
                WriteStatisticsCSV(testResults, statsCsvOutputPath);
#endif
            if (csvOutputPath != null)
                WriteTestResultsCSV(testResults, csvOutputPath);

            return 0;
        }

#if !LINUX_BUILD
        private static List<TestResultComparison> DoComparisons(List<Tuple<string, string>> allComparisonIds, Dictionary<string, Dictionary<string, TestResult>> testResults)
        {
            var comparisonResults = new List<TestResultComparison>();

            foreach (var comparisonIds in allComparisonIds)
            {
                var baseline = testResults[comparisonIds.Item1];
                var comparison = testResults[comparisonIds.Item2];

                foreach (var comparisonTest in comparison.Values)
                {
                    var baselineTest = baseline[comparisonTest.TestName];

                    // Compute the standard error in the difference
                    var baselineCount = baselineTest.Iterations.Count;
                    var baselineSum = baselineTest.Iterations.Sum(iteration => iteration.MetricValues[DurationMetricName]);
                    var baselineSumSquared = baselineSum * baselineSum;
                    var baselineSumOfSquares = baselineTest.Iterations.Sum(iteration => iteration.MetricValues[DurationMetricName] * iteration.MetricValues[DurationMetricName]);

                    var comparisonCount = comparisonTest.Iterations.Count;
                    var comparisonSum = comparisonTest.Iterations.Sum(iteration => iteration.MetricValues[DurationMetricName]);
                    var comparisonSumSquared = comparisonSum * comparisonSum;
                    var comparisonSumOfSquares = comparisonTest.Iterations.Sum(iteration => iteration.MetricValues[DurationMetricName] * iteration.MetricValues[DurationMetricName]);

                    var stdErrorDiff = Math.Sqrt((baselineSumOfSquares - (baselineSumSquared / baselineCount) + comparisonSumOfSquares - (comparisonSumSquared / comparisonCount)) * (1.0 / baselineCount + 1.0 / comparisonCount) / (baselineCount + comparisonCount - 1));
                    var interval = stdErrorDiff * MathNet.Numerics.ExcelFunctions.TInv(1.0 - ErrorConfidence, baselineCount + comparisonCount - 2);

                    RunningStatistics comparisonStats = comparisonTest.Stats[DurationMetricName].RunningStatistics;
                    RunningStatistics baselineStats = baselineTest.Stats[DurationMetricName].RunningStatistics;
                    var comparisonResult = new TestResultComparison();
                    comparisonResult.BaselineResult = baselineTest;
                    comparisonResult.ComparisonResult = comparisonTest;
                    comparisonResult.TestName = comparisonTest.TestName;
                    comparisonResult.PercentChange = (comparisonStats.Mean - baselineStats.Mean) / baselineStats.Mean;
                    comparisonResult.PercentChangeError = interval / baselineStats.Mean;

                    comparisonResults.Add(comparisonResult);
                }
            }

            return comparisonResults;
        }

        private static XDocument WriteTestResultsXml(Dictionary<string, Dictionary<string, TestResult>> testResults, List<TestResultComparison> comparisonResults)
        {
            var resultElem = new XElement("results");
            var xmlDoc = new XDocument(resultElem);

            foreach (var run in testResults)
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
                            new XAttribute("marginOfError", runningStats.MarginOfError(ErrorConfidence).ToString("G3")),
                            new XAttribute("stddev", runningStats.StandardDeviation.ToString("G3"))));
                    }
                }
            }

            foreach (var comparison in comparisonResults)
            {
                var comparisonElem = new XElement("comparison", new XAttribute("test", comparison.TestName), new XAttribute("baselineId", comparison.BaselineResult.RunId), new XAttribute("comparisonId", comparison.ComparisonResult.RunId));
                resultElem.Add(comparisonElem);

                comparisonElem.Add(
                    new XElement("duration",
                        new XAttribute("changeRatio", comparison.PercentChange.ToString("G3")),
                        new XAttribute("changeRatioError", comparison.PercentChangeError.ToString("G3"))));
            }

            return xmlDoc;
        }

        private static void WriteTestResultsHtml(Dictionary<string, Dictionary<string, TestResult>> testResults, List<TestResultComparison> comparisonResults, string htmlOutputPath)
        {
            using (var writer = new StreamWriter(htmlOutputPath, false, Encoding.UTF8))
            {
                writer.WriteLine("<html><body>");

                foreach (var comparison in comparisonResults.GroupBy(r => $"Comparison: {r.ComparisonResult.RunId} | Baseline: {r.BaselineResult.RunId}"))
                {
                    writer.WriteLine($"<h1>{comparison.Key}</h1>");
                    writer.WriteLine("<table>");
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
                        writer.WriteLine($"<tr><td>{test.TestName}</td><td><font  color={color}>{test.PercentChange.ToString("+##.#%;-##.#%")}</font></td><td>+/-{test.PercentChangeError.ToString("P1")}</td></tr>");
                    }
                    writer.WriteLine("</table>");
                }

                writer.WriteLine("<hr>");

                foreach (var run in testResults)
                {
                    writer.WriteLine($"<h1>Indivdual results: {run.Key}</h1>");

                    writer.WriteLine($"<table>");
                    writer.WriteLine($"<tr><th>Test</th><th>Unit</th><th>Min</th><th>Mean</th><th>Max</th><th>Margin</th><th>StdDev</th><th>Iterations</th></tr>");
                    foreach (var test in run.Value)
                    {
                        var stats = test.Value.Stats[DurationMetricName].RunningStatistics;
                        writer.WriteLine($"<tr><td>{test.Value.TestName}</td><td>ms</td><td>{stats.Minimum.ToString("G3")}</td><td>{stats.Mean.ToString("G3")}</td><td>{stats.Maximum.ToString("G3")}</td><td>{stats.MarginOfError(ErrorConfidence).ToString("P1")}</td><td>{stats.StandardDeviation.ToString("G3")}</td><td>{test.Value.Stats[DurationMetricName].IterationCount.ToString()}</td></tr>");
                    }
                    writer.WriteLine($"</table>");
                }

                writer.WriteLine("</html></body>");
            }
        }


        private static void WriteStatisticsCSV(Dictionary<string, Dictionary<string, TestResult>> testResults, string analyzeOutputPath)
        {
            using (var writer = new StreamWriter(analyzeOutputPath))
            {
                writer.WriteLine("Test, Iterations, Duration Min, Duration Max, Duration Average, Duration Stdev, Metrics");
                foreach (var run in testResults)
                {
                    foreach (var result in run.Value.Values)
                    {
                        RunningStatistics durationStats = result.Stats[DurationMetricName].RunningStatistics;
                        writer.WriteLine(
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
        }

#endif


        private static Dictionary<string, Dictionary<string, TestResult>> SummarizeTestResults(IEnumerable<TestIterationResult> allIterations)
        {
            var testResults = new Dictionary<string, Dictionary<string, TestResult>>();

            foreach (var iteration in allIterations)
            {
                Dictionary<string, TestResult> runResults;
                if (!testResults.TryGetValue(iteration.RunId, out runResults))
                    testResults[iteration.RunId] = runResults = new Dictionary<string, TestResult>();

                TestResult result;
                if (!runResults.TryGetValue(iteration.TestName, out result))
                {
                    runResults[iteration.TestName] = result = new TestResult();
                    result.RunId = iteration.RunId;
                    result.TestName = iteration.TestName;
                }

                foreach (var metric in iteration.MetricValues)
                {
                    TestStatistics stats;
                    if (!result.Stats.TryGetValue(metric.Key, out stats))
                        result.Stats[metric.Key] = stats = new TestStatistics();
                    stats.Push(metric.Value);
                }

                result.Iterations.Add(iteration);
            }

            return testResults;
        }


        private static string EscapeCsvString(string str)
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


        private static void WriteTestResultsCSV(Dictionary<string, Dictionary<string, TestResult>> testResults, string csvOutputPath)
        {
            using (var writer = new StreamWriter(new FileStream(csvOutputPath, FileMode.OpenOrCreate)))
            {
                foreach (var run in testResults)
                {
                    foreach (var result in run.Value.Values)
                    {
                        foreach (var iteration in result.Iterations)
                        {
                            writer.WriteLine(String.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\"",
                                EscapeCsvString(iteration.RunId), EscapeCsvString(iteration.RunId), EscapeCsvString(iteration.TestName), iteration.MetricValues[DurationMetricName].ToString()));
                        }
                    }
                }
            }
        }


        private static string GetMetricsString(Dictionary<string, TestStatistics>.KeyCollection metrics)
        {
            StringBuilder builder = new StringBuilder();
            foreach (string metric in metrics)
            {
                builder.AppendFormat("{0};", metric);
            }

            return builder.ToString();
        }



        private static IEnumerable<string> ExpandFilePath(string path)
        {
            if (File.Exists(path))
            {
                yield return path;
            }
            else if (Directory.Exists(path))
            {
                foreach (var file in Directory.EnumerateFiles(path, "*.xml", SearchOption.AllDirectories))
                    yield return file;
            }
        }

        private class TestResult
        {
            public string TestName;
            public string RunId;
            public Dictionary<string, TestStatistics> Stats = new Dictionary<string, TestStatistics>();
            public List<TestIterationResult> Iterations = new List<TestIterationResult>();
        }

        private class TestIterationResult
        {
            public string EtlPath;
            public string RunId;
            public string TestName;
            public int TestIteration;
            public Dictionary<string, double> MetricValues = new Dictionary<string, double>();
        }

        private class TestResultComparison
        {
            public string TestName;
            public TestResult BaselineResult;
            public TestResult ComparisonResult;
            public double PercentChange;
            public double PercentChangeError;
            public double SortChange => (PercentChange > 0) ? Math.Max(PercentChange - PercentChangeError, 0) : Math.Min(PercentChange + PercentChangeError, 0);
            public bool? Passed
            {
                get
                {
                    if (PercentChange > 0 && PercentChange > PercentChangeError)
                        return false;
                    if (PercentChange < 0 && PercentChange < -PercentChangeError)
                        return true;
                    else
                        return null;
                }
            }
        }

        private static IEnumerable<TestIterationResult> ParseXmlFiles(IEnumerable<string> etlPaths)
        {
            return
                from path in etlPaths.AsParallel()
                from result in ParseOneXmlFile(path)
                select result;
        }

        private static IEnumerable<TestIterationResult> ParseOneXmlFile(string path)
        {
            Console.WriteLine($"Parsing {path}");

            var doc = XDocument.Load(path);

            foreach (var testElem in doc.Descendants("test"))
            {
                var testName = testElem.Attribute("name").Value;

                var perfElem = testElem.Element("performance");
                var runId = perfElem.Attribute("runid").Value;
                var etlPath = perfElem.Attribute("etl").Value;

                foreach (var iteration in perfElem.Descendants("iteration"))
                {
                    var index = int.Parse(iteration.Attribute("index").Value);

                    if (index == 0)
                    {
                        continue;
                    }

                    var result = new TestIterationResult();
                    result.TestName = testName;
                    result.TestIteration = index;
                    result.RunId = runId;
                    result.EtlPath = etlPath;

                    foreach (var metricAttr in iteration.Attributes().Where(a => a.Name != "index"))
                    {
                        var metricName = metricAttr.Name.LocalName;
                        var metricVal = double.Parse(metricAttr.Value);

                        result.MetricValues.Add(metricName, metricVal);
                    }

                    yield return result;
                }
            }
        }
    }
}
