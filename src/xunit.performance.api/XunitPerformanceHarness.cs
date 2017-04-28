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
        public XunitPerformanceHarness(string[] args)
        {
            _args = new string[args.Length];
            args.CopyTo(_args, 0);

            _disposed = false;
            _outputFiles = new List<string>();

            var options = XunitPerformanceHarnessOptions.Parse(_args);

            OutputDirectory = options.OutputDirectory;
            _metricCollectionFactory = GetPerformanceMetricFactory(options.MetricNames);
            _requireEtw = RequireEtw(options.MetricNames);
            _typeNames = new List<string>(options.TypeNames);

            Configuration.RunId = options.RunId;
            Configuration.FileLogPath = Configuration.RunId + ".csv"; // TODO: Conditionally set this based on whether we want a csv file written.
        }

        public string OutputDirectory { get; }

        public IEnumerable<string> TypeNames => _typeNames.AsReadOnly();

        public BenchmarkConfiguration Configuration => BenchmarkConfiguration.Instance;

        public void RunBenchmarks(string assemblyFileName)
        {
            Validate(assemblyFileName);

            Action<string> xUnitAction = (assemblyPath) => { XunitRunner.Run(assemblyPath, _typeNames); };
            var xUnitPerformanceSessionData = new XUnitPerformanceSessionData {
                AssemblyFileName = assemblyFileName,
                CollectOutputFilesCallback = (fileName) => {
                    // FIXME: This will need safe guards when the client calls RunBenchmarks in different threads.
                    _outputFiles.Add(fileName);
                    WriteInfoLine($"File saved to: \"{fileName}\"");
                },
                OutputDirectory = OutputDirectory,
                RunId = Configuration.RunId
            };

            if (IsWindowsPlatform && _requireEtw)
            {
                Action winRunner = () => { xUnitAction(assemblyFileName); };
                ETWProfiler.Record(
                    xUnitPerformanceSessionData,
                    XunitBenchmark.GetMetadata(
                        assemblyFileName,
                        _metricCollectionFactory.GetMetrics(assemblyFileName)),
                    winRunner);
            }
            else
            {
                xUnitAction.Invoke(assemblyFileName);
                ProcessResults(xUnitPerformanceSessionData);
            }
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

                // TODO: Start scenario profiling.

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

                // TODO: Stop scenario profiling.

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

        private void ProcessResults(XUnitPerformanceSessionData xUnitSessionData)
        {
            var reader = new CSVMetricReader(Configuration.FileLogPath);
            var fileNameWithoutExtension = $"{xUnitSessionData.RunId}-{Path.GetFileNameWithoutExtension(xUnitSessionData.AssemblyFileName)}";
            var statisticsFileName = $"{fileNameWithoutExtension}-Statistics";
            var mdFileName = Path.Combine(xUnitSessionData.OutputDirectory, $"{statisticsFileName}.md");

            var assemblyModel = AssemblyModel.Create(xUnitSessionData.AssemblyFileName, reader);
            var xmlFileName = Path.Combine(xUnitSessionData.OutputDirectory, $"{fileNameWithoutExtension}.xml");
            new AssemblyModelCollection { assemblyModel }.Serialize(xmlFileName);
            xUnitSessionData.CollectOutputFilesCallback(xmlFileName);

            var dt = assemblyModel.GetStatistics();
            var mdTable = MarkdownHelper.GenerateMarkdownTable(dt);
            MarkdownHelper.Write(mdFileName, mdTable);
            xUnitSessionData.CollectOutputFilesCallback(mdFileName);
            Console.WriteLine(MarkdownHelper.ToTrimmedTable(mdTable));

            var csvFileName = Path.Combine(xUnitSessionData.OutputDirectory, $"{statisticsFileName}.csv");
            dt.WriteToCSV(csvFileName);
            xUnitSessionData.CollectOutputFilesCallback(csvFileName);

            BenchmarkEventSource.Log.Clear();
        }

        private static IPerformanceMetricFactory GetPerformanceMetricFactory(IEnumerable<string> metricNames)
        {
            var metricCollectionFactory = new CompositePerformanceMetricFactory();

            foreach (var metricName in metricNames)
            {
                if (metricName.Equals("default", StringComparison.OrdinalIgnoreCase))
                {
                    metricCollectionFactory.Add(new DefaultPerformanceMetricFactory());
                }
                else if (metricName.Equals("gcapi", StringComparison.OrdinalIgnoreCase))
                {
                    metricCollectionFactory.Add(new GCAPIPerformanceMetricFactory());
                }
                else if (metricName.Equals("stopwatch", StringComparison.OrdinalIgnoreCase))
                {
                    metricCollectionFactory.Add(new StopwatchPerformanceMetricFactory());
                }
                else if (metricName.Equals("BranchMispredictions", StringComparison.OrdinalIgnoreCase)
                    || metricName.Equals("CacheMisses", StringComparison.OrdinalIgnoreCase)
                    || metricName.Equals("InstructionRetired", StringComparison.OrdinalIgnoreCase))
                {
                    metricCollectionFactory.Add(new PmcPerformanceMetricFactory(metricName));
                }
            }

            return metricCollectionFactory;
        }

        private static bool RequireEtw(IEnumerable<string> metricNames)
        {
            var metricsThatNeedEtw = new[] {
                "default",
                "BranchMispredictions",
                "CacheMisses",
                "InstructionRetired",
                // FIXME: We need to decouple the way events are written
                //  (ETW vs. CSV), and move this metric as close as possible to
                //  the start/stop iteration marker. Currently, this is closer
                //  to the ETW marker.
                "gcapi"
            };
            var requiredEtw = metricNames.Intersect(
                metricsThatNeedEtw, StringComparer.OrdinalIgnoreCase).Count() != 0;
            return IsWindowsPlatform && requiredEtw;
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

        private readonly string[] _args;
        private readonly List<string> _outputFiles;
        private readonly List<string> _typeNames;
        private readonly IPerformanceMetricFactory _metricCollectionFactory;
        private readonly bool _requireEtw;
        private bool _disposed;
    }
}
