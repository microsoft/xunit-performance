using Microsoft.Xunit.Performance.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.MicrosoftXunitBenchmark;
using System.Xml.Linq;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;

namespace Microsoft.Xunit.Performance
{
    class PerformanceMetricEvaluationContextImpl : PerformanceMetricEvaluationContext, IDisposable
    {
        private readonly Dictionary<string, List<PerformanceMetricEvaluator>> _evaluators = new Dictionary<string, List<PerformanceMetricEvaluator>>();
        private readonly Dictionary<string, List<List<PerformanceMetricValue>>> _metricValues = new Dictionary<string, List<List<PerformanceMetricValue>>>();
        private readonly TraceEventSource _traceEventSource;
        private readonly HashSet<int> _currentProcesses = new HashSet<int>();
        private string _currentTestCase;
        private int _currentIteration;

        internal List<List<PerformanceMetricValue>> GetValues(string testCase)
        {
            return _metricValues.GetOrDefault(testCase);
        }

        public override TraceEventSource TraceEventSource => _traceEventSource;

        public override bool IsTestEvent(TraceEvent traceEvent) => _currentProcesses.Contains(traceEvent.ProcessID);

        internal PerformanceMetricEvaluationContextImpl(TraceEventSource traceEventSource, IEnumerable<PerformanceTestInfo> testInfo)
        {
            _traceEventSource = traceEventSource;

            var benchmarkParser = new MicrosoftXunitBenchmarkTraceEventParser(traceEventSource);
            benchmarkParser.BenchmarkIterationStart += BenchmarkIterationStart;
            benchmarkParser.BenchmarkIterationStop += BenchmarkIterationStop;

            traceEventSource.Kernel.ProcessStart += ProcessStart;
            traceEventSource.Kernel.ProcessStop += ProcessStop;

            foreach (var info in testInfo)
            {
                var evaluators = info.Metrics.Select(m => m.CreateEvaluator(this)).ToList();
                _evaluators[info.TestCase.DisplayName] = evaluators;
            }
        }

        private void BenchmarkIterationStart(BenchmarkIterationStartArgs args)
        {
            if (_currentTestCase != null)
                throw new InvalidOperationException();

            _currentTestCase = args.BenchmarkName;
            _currentIteration = args.Iteration;
            _currentProcesses.Add(args.ProcessID);

            foreach (var evaluator in _evaluators[_currentTestCase])
                evaluator.BeginIteration();
        }

        private void BenchmarkIterationStop(BenchmarkIterationStopArgs args)
        {
            if (_currentTestCase != args.BenchmarkName)
                throw new InvalidOperationException();

            foreach (var evaluator in _evaluators[_currentTestCase])
            {
                var values = evaluator.EndIteration();
                var allValues = _metricValues.GetOrAdd(_currentTestCase);

                while (allValues.Count <= args.Iteration)
                    allValues.Add(null);

                allValues[args.Iteration] = values.ToList();
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
                    evaluator.Dispose();
        }
    }
}
