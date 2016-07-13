// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !LINUX_BUILD
using MathNet.Numerics.Statistics;
using System.Runtime.Serialization;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.Xunit.Performance.Analysis
{
    internal class Program
    {
        /// <summary>
        /// The set of metrics (name of the metric) that appear in the .etl files
        /// </summary>

        private static int Usage()
        {
            Console.Error.WriteLine(
                "usage: xunit.performance.analysis <xmlPaths> [-compare \"baselineRunId\" \"comparisonRunId\"]  [-xml <output.xml>] [-html <output.html>] [-csvdir Directory where csvs will be generated]");
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

                        case "csvdir":
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
                            if (file != xmlOutputPath)
                            {
                                foundFile = true;
                                xmlPaths.Add(file);
                            }
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

            //
            // Start analyzing...
            //

            var allIterations = ParseXmlFiles(xmlPaths);

            HashSet<string> metricsFound;
            var testResults = SummarizeTestResults(allIterations, out metricsFound);

#if !LINUX_BUILD
            var comparisonResults = DoComparisons(allComparisonIds, testResults);

            if (xmlOutputPath != null)
            {
                new XmlResultsWriter(Properties.AllMetrics, testResults, comparisonResults, xmlOutputPath).Write();
            }

            if (htmlOutputPath != null)
            {
                new HtmlResultsWriter(Properties.AllMetrics, testResults, comparisonResults, htmlOutputPath).Write();
            }

            if (statsCsvOutputPath != null)
            {
                new CsvStatsWriter(Properties.AllMetrics, testResults, comparisonResults, statsCsvOutputPath).Write();
            }
#endif

            if (csvOutputPath != null)
            {
                if (metricsFound.Contains(Properties.DurationMetricName))
                    new CsvResultsWriter(Properties.AllMetrics, testResults, null, csvOutputPath, Properties.DurationMetricName).Write();

                if (metricsFound.Contains(Properties.GCAllocMetricName))
                    new CsvResultsWriter(Properties.AllMetrics, testResults, null, csvOutputPath, Properties.GCAllocMetricName).Write();

                if (metricsFound.Contains(Properties.GCCountMetricName))
                    new CsvResultsWriter(Properties.AllMetrics, testResults, null, csvOutputPath, Properties.GCCountMetricName).Write();

                if (metricsFound.Contains(Properties.InstRetiredMetricName))
                    new CsvResultsWriter(Properties.AllMetrics, testResults, null, csvOutputPath, Properties.InstRetiredMetricName).Write();
            }

            return 0;
        }

#if !LINUX_BUILD
        private static Dictionary<string, List<TestResultComparison>> DoComparisons(List<Tuple<string, string>> allComparisonIds, Dictionary<string, Dictionary<string, TestResult>> testResults)
        {
            var comparisonMatrix = new Dictionary<string, List<TestResultComparison>>();

            foreach (var comparisonIds in allComparisonIds)
            {
                var baseline = testResults[comparisonIds.Item1];
                var comparison = testResults[comparisonIds.Item2];

                //
                // Analize results metric-by-metric
                //
                foreach (var metricName in Properties.AllMetrics.Keys)
                {
                    var comparisonResults = new List<TestResultComparison>();

                    comparisonMatrix.Add(metricName, comparisonResults);

                    foreach (var comparisonTest in comparison.Values)
                    {
                        var baselineTest = baseline[comparisonTest.TestName];

                        var baselineCount = baselineTest.Iterations.Count;
                        var comparisonCount = comparisonTest.Iterations.Count;

                        if (baselineCount <= 0 || comparisonCount <= 0)
                        {
                            continue;
                        }

                        if (!baselineTest.Iterations[0].MetricValues.ContainsKey(metricName) ||
                            !comparisonTest.Iterations[0].MetricValues.ContainsKey(metricName))
                        {
                            continue;
                        }

                        // Compute the standard error in the difference
                        var baselineSum = baselineTest.Iterations.Sum(iteration => iteration.MetricValues[metricName]);
                        var baselineSumSquared = baselineSum * baselineSum;
                        var baselineSumOfSquares = baselineTest.Iterations.Sum(iteration => iteration.MetricValues[metricName] * iteration.MetricValues[metricName]);

                        var comparisonSum = comparisonTest.Iterations.Sum(iteration => iteration.MetricValues[metricName]);
                        var comparisonSumSquared = comparisonSum * comparisonSum;
                        var comparisonSumOfSquares = comparisonTest.Iterations.Sum(iteration => iteration.MetricValues[metricName] * iteration.MetricValues[metricName]);

                        var stdErrorDiff = Math.Sqrt((baselineSumOfSquares - (baselineSumSquared / baselineCount) + comparisonSumOfSquares - (comparisonSumSquared / comparisonCount)) * (1.0 / baselineCount + 1.0 / comparisonCount) / (baselineCount + comparisonCount - 1));
                        var interval = stdErrorDiff * MathNet.Numerics.ExcelFunctions.TInv(1.0 - Properties.ErrorConfidence, baselineCount + comparisonCount - 2);

                        RunningStatistics comparisonStats = comparisonTest.Stats[metricName].RunningStatistics;
                        RunningStatistics baselineStats = baselineTest.Stats[metricName].RunningStatistics;
                        var comparisonResult = new TestResultComparison();
                        comparisonResult.BaselineResult = baselineTest;
                        comparisonResult.ComparisonResult = comparisonTest;
                        comparisonResult.TestName = comparisonTest.TestName;
                        comparisonResult.PercentChange = (comparisonStats.Mean - baselineStats.Mean) / baselineStats.Mean;
                        comparisonResult.PercentChangeError = interval / baselineStats.Mean;

                        comparisonResults.Add(comparisonResult);
                    }
                }
            }

            return comparisonMatrix;
        }

#endif


        private static Dictionary<string, Dictionary<string, TestResult>> SummarizeTestResults(IEnumerable<TestIterationResult> allIterations, out HashSet<string> metricsFound)
        {
            var testResults = new Dictionary<string, Dictionary<string, TestResult>>();
            metricsFound = new HashSet<string>();

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
                    {
                        result.Stats[metric.Key] = stats = new TestStatistics();
                        metricsFound.Add(metric.Key);
                    }

                    stats.Push(metric.Value);
                }

                result.Iterations.Add(iteration);
            }

            return testResults;
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


        private static IEnumerable<TestIterationResult> ParseXmlFiles(IEnumerable<string> etlPaths)
        {
            var results = new List<TestIterationResult>();

            foreach (var path in etlPaths)
            {
                Console.Write($"Parsing {path}...");

                try
                {
                    using (var reader = XmlReader.Create(path))
                    {
                        var result = ParseOneXmlFile(XDocument.Load(reader));

                        if (result != null && result.Count<TestIterationResult>() > 0)
                        {
                            results.AddRange(result);

                            Console.WriteLine("done!");
                        }
                        else
                        {
                            Console.WriteLine("malformed log or not an ETW log.");
                        }
                    }
                }
                catch
                {
                    Console.WriteLine("exception!");
                }
            }

            return results;
        }

        private static IEnumerable<TestIterationResult> ParseOneXmlFile(XDocument doc)
        {
            foreach (var testElem in doc.Descendants("test"))
            {
                var testName = testElem.Attribute("name").Value;

                var perfElem = testElem.Element("performance");
                var runId = perfElem.Attribute("runid").Value;
                var etlPath = perfElem.Attribute("etl").Value;

                foreach (var metrics in perfElem.Descendants("metrics"))
                {
                    foreach (var metric in metrics.Elements())
                    {
                        var metricName = metric.Name.LocalName;
                        var unit = metric.Attribute("unit").Value;

                        //
                        // Populate the set of all collected metrics 
                        //
                        if (!Properties.AllMetrics.ContainsKey(metricName))
                        {
                            Properties.AllMetrics.Add(metricName, unit);
                        }
                    }
                }

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
