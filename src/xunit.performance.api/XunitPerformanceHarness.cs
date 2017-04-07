using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static Microsoft.Xunit.Performance.Api.Common;
using static Microsoft.Xunit.Performance.Api.PerformanceLogger;

namespace Microsoft.Xunit.Performance.Api
{
    public sealed class XunitPerformanceHarness : IDisposable
    {
        static XunitPerformanceHarness()
        {
            Action<string, string, string, Action, Action<string>> etwProfiler = (assemblyPath, runId, outputDirectory, runner, collectOutputFilesCallback) => {
                ETWProfiler.Record(assemblyPath, runId, outputDirectory, runner, collectOutputFilesCallback);
            };
            Action<string, string, string, Action, Action<string>> genericProfiler = (assemblyPath, runId, outputDirectory, runner, collectOutputFilesCallback) => {
                GenericProfiler.Record(assemblyPath, runId, outputDirectory, runner, collectOutputFilesCallback);
            };
            s_profiler = IsWindowsPlatform ? etwProfiler : genericProfiler;
        }

        public XunitPerformanceHarness(string[] args)
        {
            _args = new string[args.Length];
            args.CopyTo(_args, 0);

            _disposed = false;
            _outputFiles = new List<string>();

            var options = XunitPerformanceHarnessOptions.Parse(_args);

            OutputDirectory = options.OutputDirectory;
            _typeNames = new List<string>(options.TypeNames);
            _runner = (assemblyPath) => {
                XunitRunner.Run(assemblyPath, _typeNames);
            };

            Configuration.RunId = options.RunId;
            Configuration.FileLogPath = Configuration.RunId + ".csv"; // TODO: Conditionally set this based on whether we want a csv file written.
        }

        public string OutputDirectory { get; }

        public IEnumerable<string> TypeNames => _typeNames.AsReadOnly();

        public BenchmarkConfiguration Configuration => BenchmarkConfiguration.Instance;

        public void RunBenchmarks(string assemblyPath)
        {
            Validate(assemblyPath);

            Action<string> collectOutputFilesCallback = fileName => {
                // FIXME: This will need safe guards when the client calls RunBenchmarks in different threads.
                _outputFiles.Add(fileName);
            };

            Action winRunner = () => {
                _runner(assemblyPath);
            };
            Action nixRunner = () => {
                _runner(assemblyPath);
                ProcessResults(assemblyPath, Configuration.RunId, OutputDirectory, collectOutputFilesCallback);
            };
            Action runner = IsWindowsPlatform ? winRunner : nixRunner;

            s_profiler(assemblyPath, Configuration.RunId, OutputDirectory, runner, collectOutputFilesCallback);
        }

        /// <summary>
        /// Executes the benchmark scenario specified by the parameter
        /// containing the process start information.<br/>
        /// The process component will wait, for the benchmark scenario to exit,
        /// the time specified on the configuration argument.<br/>
        /// If the benchmark scenario has not exited, then it will immediately
        /// stop the associated process, and a TimeoutException will be thrown.
        /// </summary>
        /// <param name="processStartInfo">The ProcessStartInfo that contains the information that is used to start the benchmark scenario process.</param>
        /// <param name="preIterationDelegate">The action that will be executed before every benchmark scenario execution.</param>
        /// <param name="postIterationDelegate">The action that will be executed after every benchmark scenario execution.</param>
        /// <param name="teardownDelegate">The action that will be executed after running all benchmark scenario iterations.</param>
        /// <param name="scenarioConfiguration">ScenarioConfiguration object that defined the scenario execution.</param>
        public void RunScenario(
            ProcessStartInfo processStartInfo,
            Action preIterationDelegate,
            Action postIterationDelegate,
            Func<ScenarioBenchmark> teardownDelegate,
            ScenarioConfiguration scenarioConfiguration)
        {
            if (processStartInfo == null)
                throw new ArgumentNullException($"{nameof(processStartInfo)} cannot be null.");
            if (teardownDelegate == null)
                throw new ArgumentNullException($"{nameof(teardownDelegate)} cannot be null.");
            if (scenarioConfiguration == null)
                throw new ArgumentNullException($"{nameof(scenarioConfiguration)} cannot be null.");

            // Make a copy of the user input to avoid potential bugs due to changes behind the scenes.
            var configuration = new ScenarioConfiguration(scenarioConfiguration);

            for (int i = 0; i < configuration.Iterations; ++i)
            {
                preIterationDelegate?.Invoke();

                // TODO: Start profiling.

                using (var process = new Process())
                {
                    process.StartInfo = processStartInfo;
                    process.Start();
                    if (process.WaitForExit((int)(configuration.TimeoutPerIteration.TotalMilliseconds)) == false)
                    {
                        process.Kill();
                        throw new TimeoutException("Running benchmark scenario has timed out.");
                    }

                    // Check for the exit code.
                    if (!configuration.SuccessExitCodes.Contains(process.ExitCode))
                        throw new Exception($"'{processStartInfo.FileName}' exited with an invalid exit code: {process.ExitCode}");
                }

                // TODO: Stop profiling.

                postIterationDelegate?.Invoke();
            }

            ScenarioBenchmark scenarioBenchmark = teardownDelegate();
            if (scenarioBenchmark == null)
                throw new InvalidOperationException("The Teardown Delegate should return a valid instance of ScenarioBenchmark.");
            scenarioBenchmark.Serialize(Configuration.RunId + "-" + scenarioBenchmark.Namespace + ".xml");

            var dt = scenarioBenchmark.GetStatistics();
            var mdTable = MarkdownHelper.GenerateMarkdownTable(dt);

            var mdFileName = $"{Configuration.RunId}-{scenarioBenchmark.Namespace}-Statistics.md";
            MarkdownHelper.Write(mdFileName, mdTable);
            WriteInfoLine($"Markdown file saved to \"{mdFileName}\"");
            Console.WriteLine(MarkdownHelper.ToTrimmedTable(mdTable));

            var csvFileName = $"{Configuration.RunId}-{scenarioBenchmark.Namespace}-Statistics.csv";
            dt.WriteToCSV(csvFileName);
            WriteInfoLine($"Statistics written to \"{csvFileName}\"");
        }

        public static string Usage()
        {
            return XunitPerformanceHarnessOptions.Usage();
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
            var fileNameWithoutExtension = $"{sessionName}-{Path.GetFileNameWithoutExtension(assemblyFileName)}";
            var statisticsFileName = $"{fileNameWithoutExtension}-Statistics";
            var mdFileName = Path.Combine(outputDirectory, $"{statisticsFileName}.md");

            var assemblyModel = AssemblyModel.Create(assemblyFileName, reader);
            var xmlFileName = Path.Combine(outputDirectory, $"{fileNameWithoutExtension}.xml");
            new AssemblyModelCollection { assemblyModel }.Serialize(xmlFileName);
            WriteInfoLine($"Performance results saved to \"{xmlFileName}\"");
            collectOutputFilesCallback(xmlFileName);

            var dt = assemblyModel.GetStatistics();
            var mdTable = MarkdownHelper.GenerateMarkdownTable(dt);
            MarkdownHelper.Write(mdFileName, mdTable);
            WriteInfoLine($"Markdown file saved to \"{mdFileName}\"");
            collectOutputFilesCallback(mdFileName);
            Console.WriteLine(MarkdownHelper.ToTrimmedTable(mdTable));

            var csvFileName = Path.Combine(outputDirectory, $"{statisticsFileName}.csv");
            dt.WriteToCSV(csvFileName);
            WriteInfoLine($"Statistics written to \"{csvFileName}\"");
            collectOutputFilesCallback(csvFileName);

            BenchmarkEventSource.Log.Clear();
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

        private static readonly Action<string, string, string, Action, Action<string>> s_profiler;
        private readonly Action<string> _runner;
        private readonly string[] _args;
        private readonly List<string> _outputFiles;
        private readonly List<string> _typeNames;
        private bool _disposed;
    }
}
