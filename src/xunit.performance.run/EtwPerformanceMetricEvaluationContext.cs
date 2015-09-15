// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Parsers.MicrosoftXunitBenchmark;
using Microsoft.Xunit.Performance.Sdk;

namespace Microsoft.Xunit.Performance
{
    internal class EtwPerformanceMetricEvaluationContext : PerformanceMetricEvaluationContext, IDisposable, IPerformanceMetricReader
    {
        private readonly Dictionary<string, List<KeyValuePair<PerformanceMetric, PerformanceMetricEvaluator>>> _evaluators = new Dictionary<string, List<KeyValuePair<PerformanceMetric, PerformanceMetricEvaluator>>>();
        private readonly Dictionary<string, List<Dictionary<string, double>>> _metricValues = new Dictionary<string, List<Dictionary<string, double>>>();
        private readonly TraceEventSource _traceEventSource;
        private readonly HashSet<int> _currentProcesses = new HashSet<int>();
        private readonly string _runid;
        private string _currentTestCase;
        private int _currentIteration;

        public string LogPath { get; }

        public IEnumerable<PerformanceMetricInfo> GetMetrics(string testCase)
        {
            return _evaluators.GetOrDefault(testCase)?.Select(kvp => kvp.Key);
        }

        public List<Dictionary<string, double>> GetValues(string testCase)
        {
            return _metricValues.GetOrDefault(testCase);
        }

        public override TraceEventSource TraceEventSource => _traceEventSource;

        public override bool IsTestEvent(TraceEvent traceEvent) => _currentProcesses.Contains(traceEvent.ProcessID);

        internal EtwPerformanceMetricEvaluationContext(string logPath, TraceEventSource traceEventSource, IEnumerable<PerformanceTestInfo> testInfo, string runid)
        {
            LogPath = logPath;
            _traceEventSource = traceEventSource;
            _runid = runid;

            var benchmarkParser = new MicrosoftXunitBenchmarkTraceEventParser(traceEventSource);
            benchmarkParser.BenchmarkIterationStart += BenchmarkIterationStart;
            benchmarkParser.BenchmarkIterationStop += BenchmarkIterationStop;

            traceEventSource.Kernel.ProcessStart += ProcessStart;
            traceEventSource.Kernel.ProcessStop += ProcessStop;

            foreach (var info in testInfo)
            {
                var evaluators = info.Metrics.Cast<PerformanceMetric>().Select(m => new KeyValuePair<PerformanceMetric, PerformanceMetricEvaluator>(m, m.CreateEvaluator(this))).ToList();
                _evaluators[info.TestCase.DisplayName] = evaluators;
            }
        }

        private void BenchmarkIterationStart(BenchmarkIterationStartArgs args)
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
        }

        private void BenchmarkIterationStop(BenchmarkIterationStopArgs args)
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
        }

        private void ProcessStart(ProcessTraceData args)
        {
            if (_currentProcesses.Contains(args.ParentID))
                _currentProcesses.Add(args.ProcessID);
        }

        private void ProcessStop(ProcessTraceData args)
        {
            _currentProcesses.Remove(args.ProcessID);
        }

        public void Dispose()
        {
            foreach (var evaluators in _evaluators.Values)
                foreach (var evaluator in evaluators)
                    evaluator.Value.Dispose();
        }
    }
}
