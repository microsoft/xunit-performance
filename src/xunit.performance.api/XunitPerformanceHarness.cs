using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xunit.Performance;
using Microsoft.Xunit.Performance.Api.Table;

namespace Microsoft.Xunit.Performance.Api
{
    public sealed class XunitPerformanceHarness : IDisposable
    {
        private string[] _args;

        public XunitPerformanceHarness(string[] args)
        {
            _args = args;

            // Set the run id.
            Configuration.RunId = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss");

            // Set the file log path.
            // TODO: Conditionally set this based on whether we want a csv file written.
            Configuration.FileLogPath = Configuration.RunId + ".csv";
        }

        void IDisposable.Dispose()
        {
            // Close the log when all test cases have completed execution.
            // TODO: This is a hack because we haven't found a way to close the file from within xunit.
            BenchmarkEventSource.Log.Close();

            // Process the results now that we know we're done executing tests.
            ProcessResults();
        }

        public BenchmarkConfiguration Configuration
        {
            get { return BenchmarkConfiguration.Instance; }
        }

        public void RunBenchmarks(string assemblyPath)
        {
            if (string.IsNullOrEmpty(assemblyPath))
            {
                throw new ArgumentNullException(nameof(assemblyPath));
            }

            if(!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException(assemblyPath);
            }

            // Invoke xunit to run benchmarks in the specified assembly.
            XunitRunner runner = new XunitRunner();
            runner.Run(assemblyPath);
        }

        private void ProcessResults()
        {
            CSVMetricReader reader = new CSVMetricReader(Configuration.FileLogPath);
            WriteStatisticsToFile(reader);
        }

        /// <summary>
        /// Generate CSV data (Probably not the most efficient way, e.g. How big can this become).
        /// </summary>
        /// <param name="reader"></param>
        private void WriteStatisticsToFile(CSVMetricReader reader)
        {
            var statisticsFilePath = $"{Configuration.RunId}-Statistics.csv";
            var statisticsTable = new DataTable();
            var col0_testName = statisticsTable.AddColumn("Test Name");
            var col1_metric = statisticsTable.AddColumn("Metric");
            var col2_iterations = statisticsTable.AddColumn("Iterations");
            var col3_average = statisticsTable.AddColumn("Average");
            var col4_stdevs = statisticsTable.AddColumn("Sample standard deviation");
            var col5_min = statisticsTable.AddColumn("Minimum");
            var col6_max = statisticsTable.AddColumn("Maximum");

            foreach (var testCaseName in reader.TestCases)
            {
                List<Dictionary<string, double>> iterations = reader.GetValues(testCaseName);

                var measurements = new Dictionary<string, List<double>>();
                foreach (var dict in iterations)
                {
                    foreach (var pair in dict)
                    {
                        if (!measurements.ContainsKey(pair.Key))
                            measurements[pair.Key] = new List<double>();
                        measurements[pair.Key].Add(pair.Value);
                    }
                }

                foreach (var measurement in measurements)
                {
                    var metric = measurement.Key;
                    var count = measurement.Value.Count;
                    var avg = measurement.Value.Average();
                    var stdev_s = Math.Sqrt(measurement.Value.Sum(x => Math.Pow(x - avg, 2)) / (measurement.Value.Count - 1));
                    var max = measurement.Value.Max();
                    var min = measurement.Value.Min();

                    var r = statisticsTable.AppendRow();
                    r[col0_testName] = testCaseName;
                    r[col1_metric] = metric;
                    r[col2_iterations] = count.ToString();
                    r[col3_average] = avg.ToString();
                    r[col4_stdevs] = stdev_s.ToString();
                    r[col5_min] = min.ToString();
                    r[col6_max] = max.ToString();
                }
            }

            statisticsTable.WriteToCSV(statisticsFilePath);
            Console.WriteLine($"\nStatistics written to \"{statisticsFilePath}\"\n");
        }
    }
}