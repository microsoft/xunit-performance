using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Xunit.Performance.Api.Profilers.Etw;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Microsoft.Xunit.Performance.Api.Common;
using static Microsoft.Xunit.Performance.Api.PerformanceLogger;

namespace Microsoft.Xunit.Performance.Api
{
    /// <summary>
    /// This is the main entry point of the xUnit Performance Api.
    /// It provides the functionality to run xUnit microbenchmarks and full benchmark scenarios.
    /// </summary>
    public sealed class XunitPerformanceHarness : IDisposable
    {
        readonly string[] _args;

        readonly bool _collectDefaultXUnitMetrics;

        readonly IPerformanceMetricFactory _metricCollectionFactory;

        readonly bool _requireEtw;

        readonly List<string> _typeNames;

        bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="XunitPerformanceHarness"/> class.
        /// </summary>
        /// <param name="args">String array that contains any command-line arguments passed in.</param>
        public XunitPerformanceHarness(string[] args)
        {
            _args = new string[args.Length];
            args.CopyTo(_args, 0);

            _disposed = false;

            var options = XunitPerformanceHarnessOptions.Parse(_args);

            OutputDirectory = options.OutputDirectory;
            _collectDefaultXUnitMetrics = options.MetricNames.Contains("default", StringComparer.OrdinalIgnoreCase);
            _metricCollectionFactory = GetPerformanceMetricFactory(options.MetricNames);
            _requireEtw = RequireEtw(options.MetricNames);
            _typeNames = new List<string>(options.TypeNames);

            Configuration.RunId = options.RunId;
            Configuration.FileLogPath = $"{Configuration.RunId}.csv"; // TODO: Conditionally set this based on whether we want a csv file written.
        }

        /// <summary>
        ///
        /// </summary>
        public BenchmarkConfiguration Configuration => BenchmarkConfiguration.Instance;

        /// <summary>
        /// Gets the path for the output directory.
        /// </summary>
        public string OutputDirectory { get; }

        /// <summary>
        /// Gets a collection of the type names of the test classes to run.
        /// </summary>
        /// <remarks>Returns an empty collection if the type names were not specified.</remarks>
        public IEnumerable<string> TypeNames => _typeNames.AsReadOnly();

        public static string Usage() => XunitPerformanceHarnessOptions.Usage();

        /// <summary>
        /// Run the xUnit tests tagged with the [<see cref="BenchmarkAttribute"/>] attribute.
        /// </summary>
        /// <param name="assemblyFileName">Path to the assembly that contains the xUnit performance tests.</param>
        public void RunBenchmarks(string assemblyFileName)
        {
            if (string.IsNullOrEmpty(assemblyFileName))
                throw new ArgumentNullException(nameof(assemblyFileName));
            if (!File.Exists(assemblyFileName))
                throw new FileNotFoundException(assemblyFileName);

            void xUnitAction(string assemblyPath)
            {
                var errCode = XunitRunner.Run(assemblyPath, _typeNames);
                if (errCode != 0)
                    throw new Exception($"{errCode} benchmark(s) failed to execute.");
            }

            var xUnitPerformanceSessionData = new XUnitPerformanceSessionData
            {
                AssemblyFileName = assemblyFileName,
                CollectOutputFilesCallback = LogFileSaved,
                OutputDirectory = OutputDirectory,
                RunId = Configuration.RunId
            };

            var xUnitPerformanceMetricData = XunitBenchmark.GetMetadata(
                assemblyFileName,
                _metricCollectionFactory.GetMetrics(),
                _collectDefaultXUnitMetrics);

            if (IsWindowsPlatform && _requireEtw)
            {
                void winRunner() { xUnitAction(assemblyFileName); }
                ETWProfiler.Record(
                    xUnitPerformanceSessionData,
                    xUnitPerformanceMetricData,
                    winRunner);
            }
            else
            {
                xUnitAction(assemblyFileName);
                ProcessResults(xUnitPerformanceSessionData, xUnitPerformanceMetricData);
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
        /// <param name="configuration">ScenarioConfiguration object that defined the scenario execution.</param>
        /// <param name="teardownDelegate">The action that will be executed after running all benchmark scenario iterations.</param>
        public void RunScenario(ScenarioTestConfiguration configuration, Action<ScenarioBenchmark> teardownDelegate)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            if (teardownDelegate == null)
                throw new ArgumentNullException(nameof(teardownDelegate));

            string testName;
            if (configuration.TestName == null)
            {
                testName = Path.GetFileNameWithoutExtension(configuration.StartInfo.FileName);
                if (configuration.Scenario == null)
                {
                    configuration.Scenario = new ScenarioBenchmark(testName);
                }
            }
            else
            {
                if (configuration.Scenario == null)
                {
                    testName = configuration.TestName;
                    configuration.Scenario = new ScenarioBenchmark(testName);
                }
                else
                {
                    testName = $"{configuration.Scenario.Name}-{configuration.TestName}";
                }
            }

            var scenarioFileName = $"{Configuration.RunId}-{testName}";
            var fileNameWithoutExtension = Path.Combine(OutputDirectory, $"{scenarioFileName}");

            for (int i = 0; i < configuration.Iterations; ++i)
            {
                using (var scenarioTest = new ScenarioTest(configuration))
                {
                    ScenarioExecutionResult scenarioExecutionResult;

                    configuration.PreIterationDelegate?.Invoke(scenarioTest);

                    WriteInfoLine($"Iteration ({i})");
                    WriteInfoLine($"  Working Directory: \"{scenarioTest.Process.StartInfo.WorkingDirectory}\"");
                    WriteInfoLine($"  Command: \"{scenarioTest.Process.StartInfo.FileName}\" {scenarioTest.Process.StartInfo.Arguments}");

                    if (IsWindowsPlatform && _requireEtw)
                    {
                        var sessionName = $"Performance-Api-Session-{Configuration.RunId}";

                        var tracesFolder = Path.Combine(OutputDirectory, $"{fileNameWithoutExtension}-traces");
                        if (!Directory.Exists(tracesFolder))
                        {
                            Directory.CreateDirectory(tracesFolder);
                        }

                        var etlFileName = Path.Combine(tracesFolder, $"{scenarioFileName}({i}).etl");

                        var userSpecifiedMetrics = _metricCollectionFactory.GetMetrics();
                        var kernelProviders = userSpecifiedMetrics
                            .SelectMany(pmi => pmi.ProviderInfo)
                            .OfType<KernelProviderInfo>()
                            .Select(kernelProviderInfo => new KernelProvider
                            {
                                Flags = (KernelTraceEventParser.Keywords)kernelProviderInfo.Keywords,
                                StackCapture = (KernelTraceEventParser.Keywords)kernelProviderInfo.StackKeywords
                            });
                        var profileSourceInfos = userSpecifiedMetrics
                            .SelectMany(pmi => pmi.ProviderInfo)
                            .OfType<CpuCounterInfo>()
                            .Where(cpuCounterInfo => Helper.AvailablePreciseMachineCounters.Keys.Contains(cpuCounterInfo.CounterName))
                            .Select(cpuCounterInfo =>
                            {
                                var profileSourceInfo = Helper.AvailablePreciseMachineCounters[cpuCounterInfo.CounterName];
                                return new ProfileSourceInfo
                                {
                                    ID = profileSourceInfo.ID,
                                    Interval = cpuCounterInfo.Interval,
                                    MaxInterval = profileSourceInfo.MaxInterval,
                                    MinInterval = profileSourceInfo.MinInterval,
                                    Name = profileSourceInfo.Name,
                                };
                            });

                        Helper.SetPreciseMachineCounters(profileSourceInfos.ToList());

                        var listener = new Listener<ScenarioExecutionResult>(
                            new SessionData(sessionName, etlFileName) { BufferSizeMB = 512 },
                            UserProvider.Defaults,
                            kernelProviders.ToList());

                        scenarioExecutionResult = listener.Record(() => { return Run(configuration, scenarioTest); });

                        scenarioExecutionResult.EventLogFileName = etlFileName;
                        scenarioExecutionResult.PerformanceMonitorCounters = userSpecifiedMetrics
                            .Where(m => Helper.AvailablePreciseMachineCounters.Keys.Contains(m.Id))
                            .Select(m =>
                            {
                                var psi = Helper.AvailablePreciseMachineCounters[m.Id];
                                return new PerformanceMonitorCounter(m.DisplayName, psi.Name, m.Unit, psi.ID);
                            })
                            .ToHashSet();

                        LogFileSaved(etlFileName);
                    }
                    else
                    {
                        scenarioExecutionResult = Run(configuration, scenarioTest);
                    }

                    configuration.PostIterationDelegate?.Invoke(scenarioExecutionResult);
                }
            }

            teardownDelegate(configuration.Scenario);

            if (configuration.SaveResults)
            {
                WriteResults(configuration.Scenario, fileNameWithoutExtension);
            }
        }

        /// <summary>
        /// Save results from an executed scenario
        /// </summary>
        /// <param name="scenario">The scenario to save results for</param>
        /// <param name="fileNameWithoutExtension">The filename (without extension) to use to save results</param>
        /// <remarks>This will save an XML, a Markdown, and a CSV file with the results.</remarks>
        public void WriteResults(ScenarioBenchmark scenario, string fileNameWithoutExtension)
        {
            WriteXmlResults(scenario, fileNameWithoutExtension);

            WriteTableResults(new[] { scenario }, fileNameWithoutExtension, false);
        }

        /// <summary>
        /// Saves Markdown and CSV results for executed scenarios
        /// </summary>
        /// <param name="scenarios">The scenarios to save results for</param>
        /// <param name="fileNameWithoutExtension">The filename (without extension) to use to save results</param>
        /// <param name="includeScenarioNameColumn">Indicates whether scenario name should be included in the tables as the first column</param>
        public void WriteTableResults(IEnumerable<ScenarioBenchmark> scenarios, string fileNameWithoutExtension, bool includeScenarioNameColumn)
        {
            var dt = ScenarioBenchmark.GetEmptyTable(includeScenarioNameColumn ? null : scenarios.First().Name);

            foreach (var scenario in scenarios)
            {
                scenario.AddRowsToTable(dt, scenario.GetStatistics(), includeScenarioNameColumn);
            }

            var mdTable = MarkdownHelper.GenerateMarkdownTable(dt);

            var csvFileName = $"{fileNameWithoutExtension}.csv";
            dt.WriteToCSV(csvFileName);
            LogFileSaved(csvFileName);

            var mdFileName = $"{fileNameWithoutExtension}.md";
            MarkdownHelper.Write(mdFileName, mdTable);
            LogFileSaved(mdFileName);
            Console.WriteLine(MarkdownHelper.ToTrimmedTable(mdTable));
        }

        /// <summary>
        /// Save results from an executed scenario in XML format
        /// </summary>
        /// <param name="scenario">The scenario to save results for</param>
        /// <param name="fileNameWithoutExtension">The filename (without extension) to use to save results</param>
        public void WriteXmlResults(ScenarioBenchmark scenario, string fileNameWithoutExtension)
        {
            var xmlFileName = $"{fileNameWithoutExtension}.xml";
            scenario.Serialize(xmlFileName);
            LogFileSaved(xmlFileName);
        }

        static IPerformanceMetricFactory GetPerformanceMetricFactory(IEnumerable<string> metricNames)
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

        /// <summary>
        /// Helper function.
        /// Writes the the file name, followed by the current line terminator, to the standard output stream.
        /// </summary>
        /// <param name="fileName">Created file name.</param>
        static void LogFileSaved(string fileName) => WriteInfoLine($"File saved to: \"{fileName}\"");

        static bool RequireEtw(IEnumerable<string> metricNames)
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
                metricsThatNeedEtw, StringComparer.OrdinalIgnoreCase).Any();
            return IsWindowsPlatform && requiredEtw;
        }

        static ScenarioExecutionResult Run(ScenarioTestConfiguration configuration, ScenarioTest scenarioTest)
        {
            var hasStarted = scenarioTest.Process.Start();
            var startTime = DateTime.UtcNow;
            if (!hasStarted)
                throw new Exception($"Failed to start {scenarioTest.Process.ProcessName}");

            if (scenarioTest.Process.StartInfo.RedirectStandardError)
                scenarioTest.Process.BeginErrorReadLine();
            if (scenarioTest.Process.StartInfo.RedirectStandardInput)
                throw new NotSupportedException($"RedirectStandardInput is not currently supported.");
            if (scenarioTest.Process.StartInfo.RedirectStandardOutput)
                scenarioTest.Process.BeginOutputReadLine();

            var hasExited = scenarioTest.Process.WaitForExit(
                (int)(configuration.TimeoutPerIteration.TotalMilliseconds));
            var exitTime = DateTime.UtcNow;

            if (!hasExited)
            {
                // TODO: scenarioOutput.Process.Kill[All|Tree]();
                scenarioTest.Process.Kill();
                throw new TimeoutException("Running benchmark scenario has timed out.");
            }

            // Check for the exit code.
            if (!configuration.SuccessExitCodes.Contains(scenarioTest.Process.ExitCode))
                throw new Exception($"'{scenarioTest.Process.StartInfo.FileName}' exited with an invalid exit code: {scenarioTest.Process.ExitCode}");

            return new ScenarioExecutionResult(scenarioTest.Process, startTime, exitTime, configuration);
        }

        void ProcessResults(XUnitPerformanceSessionData xUnitSessionData, XUnitPerformanceMetricData xUnitPerformanceMetricData)
        {
            if (!File.Exists(Configuration.FileLogPath))
            {
                WriteWarningLine($"Results file '{Configuration.FileLogPath}' does not exist. Skipping processing of results.");
                return;
            }

            var reader = new CSVMetricReader(Configuration.FileLogPath);
            var fileNameWithoutExtension = $"{xUnitSessionData.RunId}-{Path.GetFileNameWithoutExtension(xUnitSessionData.AssemblyFileName)}";

            var assemblyModel = AssemblyModel.Create(xUnitSessionData.AssemblyFileName, reader, xUnitPerformanceMetricData);
            var xmlFileName = Path.Combine(xUnitSessionData.OutputDirectory, $"{fileNameWithoutExtension}.xml");
            new AssemblyModelCollection { assemblyModel }.Serialize(xmlFileName);
            xUnitSessionData.CollectOutputFilesCallback(xmlFileName);

            var dt = assemblyModel.GetStatistics();
            var mdTable = MarkdownHelper.GenerateMarkdownTable(dt);
            var mdFileName = Path.Combine(xUnitSessionData.OutputDirectory, $"{fileNameWithoutExtension}.md");
            MarkdownHelper.Write(mdFileName, mdTable);
            xUnitSessionData.CollectOutputFilesCallback(mdFileName);
            Console.WriteLine(MarkdownHelper.ToTrimmedTable(mdTable));

            var csvFileName = Path.Combine(xUnitSessionData.OutputDirectory, $"{fileNameWithoutExtension}.csv");
            dt.WriteToCSV(csvFileName);
            xUnitSessionData.CollectOutputFilesCallback(csvFileName);

            BenchmarkEventSource.Log.Clear();
        }

        #region IDisposable implementation

        ~XunitPerformanceHarness()
        {
            Dispose(false);
        }

        /// <summary>
        /// Performs tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                FreeUnManagedResources();
                _disposed = true;
            }
        }

        void FreeUnManagedResources()
        {
            // Close the log when all test cases have completed execution.
            // HACK: This is a hack because we haven't found a way to close the file from within xunit.
            BenchmarkEventSource.Log.Close();

            // Deleting raw data not used by api users.
            if (File.Exists(Configuration.FileLogPath))
                File.Delete(Configuration.FileLogPath);
        }

        #endregion IDisposable implementation
    }
}