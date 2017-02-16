using Microsoft.Xunit.Performance.Api.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using static Microsoft.Xunit.Performance.Api.XunitPerformanceLogger;

namespace Microsoft.Xunit.Performance.Api
{
    public sealed class XunitPerformanceHarness : IDisposable
    {
        static XunitPerformanceHarness()
        {
            s_isWindowsPlatform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            s_runner = (assemblyPath) => { XunitRunner.Run(assemblyPath); };

            Action<string, string, string, Action, Action<string>> etwProfiler = (assemblyPath, runId, outputDirectory, runner, collectOutputFilesCallback) =>
            {
                ETWProfiler.Record(assemblyPath, runId, outputDirectory, runner, collectOutputFilesCallback);
            };
            Action<string, string, string, Action, Action<string>> genericProfiler = (assemblyPath, runId, outputDirectory, runner, collectOutputFilesCallback) =>
            {
                GenericProfiler.Record(assemblyPath, runId, outputDirectory, runner, collectOutputFilesCallback);
            };
            s_profiler = s_isWindowsPlatform ? etwProfiler : genericProfiler;
        }

        public XunitPerformanceHarness(string[] args)
        {
            _args = args;
            _disposed = false;
            _outputDirectory = Directory.GetCurrentDirectory();
            _outputFiles = new List<string>();

            // TODO: parse arguments.

            // Set the run id.
            Configuration.RunId = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");

            // Set the file log path.
            // TODO: Conditionally set this based on whether we want a csv file written.
            Configuration.FileLogPath = Configuration.RunId + ".csv";
        }

        public BenchmarkConfiguration Configuration => BenchmarkConfiguration.Instance;

        public void RunBenchmarks(string assemblyPath)
        {
            Validate(assemblyPath);

            Action<string> collectOutputFilesCallback = fileName =>
            {
                // FIXME: This will need safe guards when the client calls RunBenchmarks in different threads.
                _outputFiles.Add(fileName);
            };
            Action runner = s_isWindowsPlatform
                ? (Action)(() => { s_runner(assemblyPath); })
                : (() => { s_runner(assemblyPath); ProcessResults(assemblyPath, Configuration.RunId, _outputDirectory, collectOutputFilesCallback); });
            s_profiler(assemblyPath, Configuration.RunId, _outputDirectory, runner, collectOutputFilesCallback);
        }

        private static void Validate(string assemblyPath)
        {
            if (string.IsNullOrEmpty(assemblyPath))
                throw new ArgumentNullException(nameof(assemblyPath));
            if (!File.Exists(assemblyPath))
                throw new FileNotFoundException(assemblyPath);
        }

        private void ProcessResults(string assemblyFileName, string sessionName, string outputDirectory, Action<string> collectOutputFilesCallback)
        {
            var reader = new CSVMetricReader(Configuration.FileLogPath);
            var statisticsFileName = $"{sessionName}-{Path.GetFileNameWithoutExtension(assemblyFileName)}-Statistics";
            var mdFileName = Path.Combine(outputDirectory, $"{statisticsFileName}.md");

            var dt = CreateStatistics(reader);
            var mdTable = MarkdownHelper.GenerateMarkdownTable(dt);
            MarkdownHelper.Write(mdFileName, mdTable);
            WriteInfoLine($"Markdown file saved to \"{mdFileName}\"");
            collectOutputFilesCallback(mdFileName);
            Console.WriteLine(mdTable);

            var csvFileName = Path.Combine(outputDirectory, $"{statisticsFileName}.csv");
            dt.WriteToCSV(csvFileName);
            WriteInfoLine($"Statistics written to \"{csvFileName}\"");
            collectOutputFilesCallback(csvFileName);

            BenchmarkEventSource.Log.Clear();
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
            var col3_average = statisticsTable.AddColumn("AVERAGE");
            var col4_stdevs = statisticsTable.AddColumn("STDEV.S");
            var col5_min = statisticsTable.AddColumn("MIN");
            var col6_max = statisticsTable.AddColumn("MAX");

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
                {
                }

                FreeUnManagedResources();

                _disposed = true;
            }
        }

        private void FreeUnManagedResources()
        {
            // Close the log when all test cases have completed execution.
            // HACK: This is a hack because we haven't found a way to close the file from within xunit.
            BenchmarkEventSource.Log.Close();

            // Deleting raw data not used by api users.
            File.Delete(Configuration.FileLogPath);
        }

        #endregion IDisposable implementation

        private static readonly bool s_isWindowsPlatform;
        private static readonly Action<string, string, string, Action, Action<string>> s_profiler;
        private static readonly Action<string> s_runner;

        private readonly string[] _args;
        private string _outputDirectory;
        private List<string> _outputFiles;
        private bool _disposed;
    }
}