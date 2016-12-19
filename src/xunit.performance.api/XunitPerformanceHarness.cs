using Microsoft.Xunit.Performance.Api.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Microsoft.Xunit.Performance.Api.XunitPerformanceLogger;

namespace Microsoft.Xunit.Performance.Api
{
    public sealed class XunitPerformanceHarness : IDisposable
    {
        public XunitPerformanceHarness(string[] args)
        {
            _args = args;
            _disposed = false;

            // Set the run id.
            Configuration.RunId = $"{DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss")}";

            // Set the file log path.
            // TODO: Conditionally set this based on whether we want a csv file written.
            Configuration.FileLogPath = Configuration.RunId + ".csv";
        }

        public BenchmarkConfiguration Configuration
        {
            get { return BenchmarkConfiguration.Instance; }
        }

        public int RunBenchmarks(string assemblyPath)
        {
            Validate(assemblyPath);
            int errCode = 0;
            ETWProfiler.Profile(assemblyPath, Configuration.RunId, () => { errCode = XunitRunner.Run(assemblyPath); });
            return errCode;
        }

        private static void Validate(string assemblyPath)
        {
            if (string.IsNullOrEmpty(assemblyPath))
                throw new ArgumentNullException(nameof(assemblyPath));
            if (!File.Exists(assemblyPath))
                throw new FileNotFoundException(assemblyPath);
        }

        private void ProcessResults()
        {
            var reader = new CSVMetricReader(Configuration.FileLogPath);
            WriteStatisticsToFile(reader);

#if !DEBUG
            // Deleting raw data not used by api users.
            File.Delete(Configuration.FileLogPath);
#endif
        }

        /// <summary>
        /// Generate CSV data (Probably not the most efficient way, e.g. How big can this become).
        /// </summary>
        /// <param name="reader"></param>
        private void WriteStatisticsToFile(CSVMetricReader reader)
        {
            // FIXME: Update the statistics file name.
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
                var iterations = reader.GetValues(testCaseName);
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

            var statisticsFilePath = $"{Configuration.RunId}-Statistics.csv";
            statisticsTable.WriteToCSV(statisticsFilePath);
            WriteInfoLine($"Statistics written to \"{statisticsFilePath}\"");
        }

#region IDisposable implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~XunitPerformanceHarness()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                    FreeManagedResources();
                FreeUnManagedResources();
                _disposed = true;
            }
        }

        private void FreeManagedResources()
        {
            // Close the log when all test cases have completed execution.
            // HACK: This is a hack because we haven't found a way to close the file from within xunit.
            BenchmarkEventSource.Log.Close();

            // Process the results now that we know we're done executing tests.
            ProcessResults();
        }

        private void FreeUnManagedResources()
        {
        }

#endregion IDisposable implementation

        private readonly string[] _args;
        private bool _disposed;
    }
}