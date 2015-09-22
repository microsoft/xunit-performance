// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance.Sdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Xunit.Performance
{
    internal class CSVMetricReader : IPerformanceMetricReader
    {
        private Dictionary<string, List<Dictionary<string, double>>> _values = new Dictionary<string, List<Dictionary<string, double>>>();

        public CSVMetricReader(string csvPath)
        {
            LogPath = csvPath;

            double currentIterationStart = double.NaN;

            foreach (var line in File.ReadLines(csvPath, Encoding.UTF8))
            {
                var parts = line.Split(',');

                var timestamp = double.Parse(parts[0]);
                var benchmarkName = parts[1].Replace(@"\_", ",").Replace(@"\n", "\n").Replace(@"\r", "\r").Replace(@"\\", @"\");
                var eventName = parts[2];
                switch (eventName)
                {
                    case "BenchmarkIterationStart":
                        currentIterationStart = timestamp;
                        break;

                    case "BenchmarkIterationStop":
                        var values = _values.GetOrAdd(benchmarkName);
                        var iteration = int.Parse(parts[3]);
                        while (values.Count < iteration)
                            values.Add(null);
                        var duration = timestamp - currentIterationStart;
                        values.Add(new Dictionary<string, double>() { { "Duration", duration } });
                        currentIterationStart = double.NaN;
                        break;
                }
            }
        }

        public string LogPath { get; }

        public IEnumerable<PerformanceMetricInfo> GetMetrics(string testCase)
        {
            yield return DurationMetric.Instance;
        }

        public List<Dictionary<string, double>> GetValues(string testCase)
        {
            return _values.GetOrDefault(testCase);
        }

        public void Dispose()
        {
        }

        private class DurationMetric : PerformanceMetricInfo
        {
            public static readonly DurationMetric Instance = new DurationMetric();

            private DurationMetric() : base("Duration", "Duration", PerformanceMetricUnits.Milliseconds) { }
        }
    }
}
