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
            _collectDefaultXUnitMetrics = options.MetricNames.Contains("default", StringComparer.OrdinalIgnoreCase);
            _metricCollectionFactory = GetPerformanceMetricFactory(options.MetricNames);
            _requireEtw = RequireEtw(options.MetricNames);
            _typeNames = new List<string>(options.TypeNames);

            Configuration.RunId = options.RunId;
            Configuration.FileLogPath = $"{Configuration.RunId}.csv"; // TODO: Conditionally set this based on whether we want a csv file written.
        }

        public string OutputDirectory { get; }

        public IEnumerable<string> TypeNames => _typeNames.AsReadOnly();

        public BenchmarkConfiguration Configuration => BenchmarkConfiguration.Instance;

        public void RunBenchmarks(string assemblyFileName)
        {
            if (string.IsNullOrEmpty(assemblyFileName))
                throw new ArgumentNullException(nameof(assemblyFileName));
            if (!File.Exists(assemblyFileName))
                throw new FileNotFoundException(assemblyFileName);

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

            var metrics = _metricCollectionFactory.GetMetrics();
            var xUnitPerformanceMetricData = XunitBenchmark.GetMetadata(
                assemblyFileName,
                metrics,
                _collectDefaultXUnitMetrics);

            if (IsWindowsPlatform && _requireEtw)
            {
                Action winRunner = () => { xUnitAction(assemblyFileName); };
                ETWProfiler.Record(
                    xUnitPerformanceSessionData,
                    xUnitPerformanceMetricData,
                    winRunner);
            }
            else
            {
                xUnitAction.Invoke(assemblyFileName);
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
        public void RunScenario(ScenarioConfiguration configuration, Func<ScenarioBenchmark> teardownDelegate)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            if (teardownDelegate == null)
                throw new ArgumentNullException(nameof(teardownDelegate));

            Action<string> OutputFileCallback = (fileName) => {
                WriteInfoLine($"File saved to: \"{fileName}\"");
            };

            var scenarioFileName = $"{Configuration.RunId}-{Path.GetFileNameWithoutExtension(configuration.StartInfo.FileName)}";
            var fileNameWithoutExtension = Path.Combine(OutputDirectory, $"{scenarioFileName}");

            for (int i = 0; i < configuration.Iterations; ++i)
            {
                using (var scenario = new Scenario(configuration))
                {
                    ScenarioExecutionResult scenarioExecutionResult;

                    configuration.PreIterationDelegate?.Invoke(scenario);

                    WriteInfoLine($"$ {Path.GetFileName(scenario.Process.StartInfo.FileName)} {scenario.Process.StartInfo.Arguments}");

                    if (IsWindowsPlatform && _requireEtw)
                    {
                        var sessionName = $"Performance-Api-Session-{Configuration.RunId}";
                        var etlFileName = $"{fileNameWithoutExtension}({i}).etl";

                        var userSpecifiedMetrics = _metricCollectionFactory.GetMetrics();
                        var kernelProviders = userSpecifiedMetrics
                            .SelectMany(pmi => pmi.ProviderInfo)
                            .OfType<KernelProviderInfo>()
                            .Select(kernelProviderInfo => new KernelProvider {
                                Flags = (KernelTraceEventParser.Keywords)kernelProviderInfo.Keywords,
                                StackCapture = (KernelTraceEventParser.Keywords)kernelProviderInfo.StackKeywords
                            });
                        var profileSourceInfos = userSpecifiedMetrics
                            .SelectMany(pmi => pmi.ProviderInfo)
                            .OfType<CpuCounterInfo>()
                            .Where(cpuCounterInfo => Helper.AvailablePreciseMachineCounters.Keys.Contains(cpuCounterInfo.CounterName))
                            .Select(cpuCounterInfo => {
                                var profileSourceInfo = Helper.AvailablePreciseMachineCounters[cpuCounterInfo.CounterName];
                                return new ProfileSourceInfo {
                                    ID = profileSourceInfo.ID,
                                    Interval = cpuCounterInfo.Interval,
                                    MaxInterval = profileSourceInfo.MaxInterval,
                                    MinInterval = profileSourceInfo.MinInterval,
                                    Name = profileSourceInfo.Name,
                                };
                            });

                        Helper.SetPreciseMachineCounters(profileSourceInfos.ToList());

                        var listener = new Listener<ScenarioExecutionResult>(
                            new SessionData(sessionName, etlFileName) { BufferSizeMB = 256 },
                            UserProvider.Defaults,
                            kernelProviders.ToList());

                        scenarioExecutionResult = listener.Record(() => { return Run(configuration, scenario); });

                        scenarioExecutionResult.EventLogFileName = etlFileName;
                        scenarioExecutionResult.PerformanceMonitorCounters = userSpecifiedMetrics
                            .Where(m => Helper.AvailablePreciseMachineCounters.Keys.Contains(m.Id))
                            .Select(m => {
                                var psi = Helper.AvailablePreciseMachineCounters[m.Id];
                                return new PerformanceMonitorCounter(m.DisplayName, psi.Name, m.Unit, psi.ID);
                            })
                            .ToHashSet();

                        OutputFileCallback?.Invoke(etlFileName);
                    }
                    else
                    {
                        scenarioExecutionResult = Run(configuration, scenario);
                    }

                    configuration.PostIterationDelegate?.Invoke(scenarioExecutionResult);
                }
            }

            ScenarioBenchmark scenarioBenchmark = teardownDelegate();
            if (scenarioBenchmark == null)
                throw new InvalidOperationException("The Teardown Delegate should return a valid instance of ScenarioBenchmark.");

            var xmlFileName = $"{fileNameWithoutExtension}.xml";
            scenarioBenchmark.Serialize(xmlFileName);
            OutputFileCallback?.Invoke(xmlFileName);

            var dt = scenarioBenchmark.GetStatistics();
            var mdTable = MarkdownHelper.GenerateMarkdownTable(dt);

            var csvFileName = $"{fileNameWithoutExtension}.csv";
            dt.WriteToCSV(csvFileName);
            OutputFileCallback?.Invoke(csvFileName);

            var mdFileName = $"{fileNameWithoutExtension}.md";
            MarkdownHelper.Write(mdFileName, mdTable);
            OutputFileCallback?.Invoke(mdFileName);
            Console.WriteLine(MarkdownHelper.ToTrimmedTable(mdTable));
        }

        public static string Usage()
        {
            return XunitPerformanceHarnessOptions.Usage();
        }

        private static ScenarioExecutionResult Run(ScenarioConfiguration configuration, Scenario scenario)
        {
            if (!scenario.Process.Start())
                throw new Exception($"Failed to start {scenario.Process.ProcessName}");

            if (scenario.Process.StartInfo.RedirectStandardError)
                scenario.Process.BeginErrorReadLine();
            if (scenario.Process.StartInfo.RedirectStandardInput)
                throw new NotSupportedException($"RedirectStandardInput is not currently supported.");
            if (scenario.Process.StartInfo.RedirectStandardOutput)
                scenario.Process.BeginOutputReadLine();

            if (scenario.Process.WaitForExit((int)(configuration.TimeoutPerIteration.TotalMilliseconds)) == false)
            {
                // TODO: scenarioOutput.Process.Kill[All|Tree]();
                scenario.Process.Kill();
                throw new TimeoutException("Running benchmark scenario has timed out.");
            }

            // Check for the exit code.
            if (!configuration.SuccessExitCodes.Contains(scenario.Process.ExitCode))
                throw new Exception($"'{scenario.Process.StartInfo.FileName}' exited with an invalid exit code: {scenario.Process.ExitCode}");

            return new ScenarioExecutionResult(scenario.Process);
        }

        private void ProcessResults(XUnitPerformanceSessionData xUnitSessionData, XUnitPerformanceMetricData xUnitPerformanceMetricData)
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
        private readonly bool _collectDefaultXUnitMetrics;
        private readonly bool _requireEtw;
        private bool _disposed;
    }
}
