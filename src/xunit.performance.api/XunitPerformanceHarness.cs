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

            foreach (string testCaseName in reader.TestCases)
            {
                Console.WriteLine($"\nTest Case: {testCaseName}");
                List<Dictionary<string, double>> iterations = reader.GetValues(testCaseName);

                for (int i = 0; i < iterations.Count; i++)
                {
                    Dictionary<string, double> iter = iterations[i];
                    foreach (KeyValuePair<string, double> result in iter)
                    {
                        Console.WriteLine($"Iter={i}; Metric={result.Key}; Value={result.Value}");
                    }
                }
            }

            WriteStatisticsToFile(reader);
        }

        /// <summary>
        /// This method writes the performance results to csv files.
        /// </summary>
        /// <param name="reader"></param>
        private void WriteStatisticsToFile(CSVMetricReader reader)
        {
            //Console.WriteLine();
            //Console.Write("[DEBUG] Press <Enter> to continue...");
            //Console.ReadLine();

            /*
             * Generate CSV data (Probably not the most efficient way, e.g. How big can this become).
             *  Test Name,Metric,Iterations,Average,Sample standard deviation,Minimum,Maximum
             */
            //var rawFilePath = $"{Configuration.RunId}-raw.csv";
            //var rawTable = new DataTable();

            var summaryFilePath = $"{Configuration.RunId}-Summary.csv";
            var summaryTable = new DataTable();
            var col0 = summaryTable.AddColumn("Test Name");
            var col1 = summaryTable.AddColumn("Metric");
            var col2 = summaryTable.AddColumn("Iterations");
            var col3 = summaryTable.AddColumn("Average");
            var col4 = summaryTable.AddColumn("Sample standard deviation");
            var col5 = summaryTable.AddColumn("Minimum");
            var col6 = summaryTable.AddColumn("Maximum");

            foreach (string testCaseName in reader.TestCases)
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

                    var r = summaryTable.AppendRow();
                    r[col0] = testCaseName;
                    r[col1] = metric;
                    r[col2] = count.ToString();
                    r[col3] = avg.ToString();
                    r[col4] = stdev_s.ToString();
                    r[col5] = min.ToString();
                    r[col6] = max.ToString();

                    //Console.WriteLine($"{testCaseName},{metric},{count},{avg},{stdev_s},{min},{max}");
                }
            }

            summaryTable.WriteToCSV(summaryFilePath);
            Console.WriteLine($"Created \"{summaryFilePath}\"");
        }
    }
}