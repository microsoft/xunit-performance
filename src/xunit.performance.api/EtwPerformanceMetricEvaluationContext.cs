// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Parsers.MicrosoftXunitBenchmark;
using Microsoft.Xunit.Performance.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Xunit.Performance.Api
{
    internal class EtwPerformanceMetricEvaluationContext : PerformanceMetricEvaluationContext, IDisposable, IPerformanceMetricReader
    {
        public EtwPerformanceMetricEvaluationContext(
            string logPath,
            TraceEventSource traceEventSource,
            IEnumerable<PerformanceTestMessage> testInfo,
            string runid)
        {
            LogPath = logPath;
            _evaluators = new Dictionary<string, List<KeyValuePair<PerformanceMetric, PerformanceMetricEvaluator>>>();
            _metricValues = new Dictionary<string, List<Dictionary<string, double>>>();
            _traceEventSource = traceEventSource;
            _currentProcesses = new HashSet<int>();
            _runid = runid;

            var benchmarkParser = new MicrosoftXunitBenchmarkTraceEventParser(_traceEventSource);
            benchmarkParser.BenchmarkIterationStart += delegate (BenchmarkIterationStartArgs args)
            {
                if (args.RunId != _runid)
                    return;

                if (_currentTestCase != null)
                    throw new InvalidOperationException(args.TimeStampRelativeMSec.ToString());

                _currentTestCase = args.BenchmarkName;
                _currentIteration = args.Iteration;
                _currentProcesses.Add(args.ProcessID);

                var evaluators = _evaluators.GetOrDefault(_currentTestCase);
                if (evaluators != null)
                {
                    foreach (var evaluator in _evaluators[_currentTestCase])
                        evaluator.Value.BeginIteration(args);
                }
            };
            benchmarkParser.BenchmarkIterationStop += delegate (BenchmarkIterationStopArgs args)
            {
                if (args.RunId != _runid)
                    return;

                if (_currentTestCase != args.BenchmarkName || _currentIteration != args.Iteration)
                    throw new InvalidOperationException();

                var evaluators = _evaluators.GetOrDefault(_currentTestCase);
                if (evaluators != null)
                {
                    var allValues = _metricValues.GetOrAdd(_currentTestCase);
                    while (allValues.Count < args.Iteration)
                        allValues.Add(null);

                    var values = new Dictionary<string, double>();
                    allValues.Add(values);

                    foreach (var evaluator in _evaluators[_currentTestCase])
                        values[evaluator.Key.Id] = evaluator.Value.EndIteration(args);
                }

                _currentTestCase = null;
                _currentProcesses.Clear();
            };

            // The current process was already running before the etl tracing even started.
            benchmarkParser.Source.Kernel.ProcessStart += delegate (ProcessTraceData args)
            {
                if (_currentProcesses.Contains(args.ParentID))
                    _currentProcesses.Add(args.ProcessID);
            };
            benchmarkParser.Source.Kernel.ProcessStop += delegate (ProcessTraceData args)
            {
                _currentProcesses.Remove(args.ProcessID);
            };

            foreach (var info in testInfo)
            {
                _evaluators[info.TestCase.DisplayName] = info.Metrics
                    .Cast<PerformanceMetric>()
                    .Select(m => new KeyValuePair<PerformanceMetric, PerformanceMetricEvaluator>(m, m.CreateEvaluator(this)))
                    .ToList();
            }
        }

        public IEnumerable<PerformanceMetricInfo> GetMetrics(string testCase)
        {
            return _evaluators.GetOrDefault(testCase)?.Select(kvp => kvp.Key);
        }

        public List<Dictionary<string, double>> GetValues(string testCase)
        {
            return _metricValues.GetOrDefault(testCase);
        }

        public override TraceEventSource TraceEventSource => _traceEventSource;

        public string LogPath { get; private set; }

        public override bool IsTestEvent(TraceEvent traceEvent)
        {
            return _currentProcesses.Contains(traceEvent.ProcessID);
        }

        public void Dispose()
        {
            foreach (var evaluators in _evaluators.Values)
                foreach (var evaluator in evaluators)
                    evaluator.Value.Dispose();
        }

        private readonly Dictionary<string, List<KeyValuePair<PerformanceMetric, PerformanceMetricEvaluator>>> _evaluators;
        private readonly Dictionary<string, List<Dictionary<string, double>>> _metricValues;
        private readonly TraceEventSource _traceEventSource;
        private readonly HashSet<int> _currentProcesses;
        private readonly string _runid;
        private string _currentTestCase;
        private int _currentIteration;
    }
}
