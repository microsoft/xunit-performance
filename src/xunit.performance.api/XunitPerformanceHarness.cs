using MarkdownLog;
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
            WriteStatisticsToFile(Configuration.RunId, reader);

#if !DEBUG
            // Deleting raw data not used by api users.
            File.Delete(Configuration.FileLogPath);
#endif
        }

        private static IEnumerable<(string testCaseName, string metric, IEnumerable<double> values)> GetMeasurements(CSVMetricReader reader)
        {
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
                    yield return (testCaseName, measurement.Key, measurement.Value);
            }
        }

        private static DataTable CreateStatistics(CSVMetricReader reader)
        {
            var statisticsTable = new DataTable();
            var col0_testName = statisticsTable.AddColumn("Test Name");
            var col1_metric = statisticsTable.AddColumn("Metric");
            var col2_iterations = statisticsTable.AddColumn("Iterations");
            var col3_average = statisticsTable.AddColumn("Average");
            var col4_stdevs = statisticsTable.AddColumn("Sample standard deviation");
            var col5_min = statisticsTable.AddColumn("Minimum");
            var col6_max = statisticsTable.AddColumn("Maximum");

            foreach (var (testCaseName, metric, values) in GetMeasurements(reader))
            {
                var count = values.Count();
                var avg = values.Average();
                var stdev_s = Math.Sqrt(values.Sum(x => Math.Pow(x - avg, 2)) / (values.Count() - 1));
                var max = values.Max();
                var min = values.Min();

                var newRow = statisticsTable.AppendRow();
                newRow[col0_testName] = testCaseName;
                newRow[col1_metric] = metric;
                newRow[col2_iterations] = count.ToString();
                newRow[col3_average] = avg.ToString();
                newRow[col4_stdevs] = stdev_s.ToString();
                newRow[col5_min] = min.ToString();
                newRow[col6_max] = max.ToString();
            }

            return statisticsTable;
        }

        private static void PrintStatistics(DataTable statisticsTable)
        {
            var cellValueFunctions = new Func<Row, object>[] {
                (row) => {
                    return row[statisticsTable.ColumnNames["Test Name"]];
                },
                (row) => {
                    return row[statisticsTable.ColumnNames["Metric"]];
                },
                (row) => {
                    return row[statisticsTable.ColumnNames["Iterations"]];
                },
                (row) => {
                    return row[statisticsTable.ColumnNames["Average"]];
                },
                (row) => {
                    return row[statisticsTable.ColumnNames["Sample standard deviation"]];
                },
                (row) => {
                    return row[statisticsTable.ColumnNames["Minimum"]];
                },
                (row) => {
                    return row[statisticsTable.ColumnNames["Maximum"]];
                },
            };

            var mdTable = statisticsTable.Rows.ToMarkdownTable(cellValueFunctions);
            mdTable.Columns = from column in statisticsTable.ColumnNames
                              select new TableColumn
                              {
                                  HeaderCell = new TableCell() { Text = column.Name }
                              };
            mdTable.Columns = new TableColumn[]
            {
                new TableColumn(){ HeaderCell = new TableCell() { Text = "Test Name" }, Alignment = TableColumnAlignment.Left },
                new TableColumn(){ HeaderCell = new TableCell() { Text = "Metric" }, Alignment = TableColumnAlignment.Left },
                new TableColumn(){ HeaderCell = new TableCell() { Text = "Iterations" }, Alignment = TableColumnAlignment.Center },
                new TableColumn(){ HeaderCell = new TableCell() { Text = "Average" }, Alignment = TableColumnAlignment.Right },
                new TableColumn(){ HeaderCell = new TableCell() { Text = "Sample standard deviation" }, Alignment = TableColumnAlignment.Right },
                new TableColumn(){ HeaderCell = new TableCell() { Text = "Minimum" }, Alignment = TableColumnAlignment.Right },
                new TableColumn(){ HeaderCell = new TableCell() { Text = "Maximum" }, Alignment = TableColumnAlignment.Right },
            };

            // TODO: Print table as Markdown!
            Console.WriteLine(mdTable);
        }

        /// <summary>
        /// Generate CSV data (Probably not the most efficient way, e.g. How big can this become).
        /// </summary>
        /// <param name="reader"></param>
        private static void WriteStatisticsToFile(string configurationRunId, CSVMetricReader reader)
        {
            var statisticsTable = CreateStatistics(reader);

            // Only create output statistics if there is data.
            if (statisticsTable.Rows.Count() > 0)
            {
                PrintStatistics(statisticsTable);

                var currentWorkingDirectory = Directory.GetCurrentDirectory();
                var statisticsFilePath = Path.Combine(currentWorkingDirectory, $"{configurationRunId}-Statistics.csv");
                statisticsTable.WriteToCSV(statisticsFilePath);
                WriteInfoLine($"Statistics written to \"{statisticsFilePath}\"");
            }
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