// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance.Sdk;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Microsoft.Xunit.Performance.Api
{
    sealed class CSVMetricReader
    {
        readonly Dictionary<string, List<Dictionary<string, double>>> _values;

        public CSVMetricReader(string csvPath)
        {
            _values = new Dictionary<string, List<Dictionary<string, double>>>();
            LogPath = csvPath;

            using (var stream = new FileStream(csvPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var sr = new StreamReader(stream, Encoding.UTF8))
                {
                    double currentIterationStart = double.NaN;
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();
                        var parts = line.Split(',');
                        var timestamp = double.Parse(parts[0], CultureInfo.InvariantCulture);
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

                            case "BenchmarkStart":
                            case "BenchmarkStop":
                                break;

                            default:
                                throw new Exception($"Found unknown event: \"{eventName}\", on \"{csvPath}\"");
                        }
                    }
                }
            }
        }

        public string LogPath { get; }

        public IEnumerable<string> TestCases
        {
            get
            {
                foreach (string testCaseName in _values.Keys)
                {
                    yield return testCaseName;
                }
            }
        }

        public IEnumerable<PerformanceMetricInfo> GetMetrics(string testCase)
        {
            yield return DurationMetric.Instance;
        }

        public List<Dictionary<string, double>> GetValues(string testCase) => _values.GetOrDefault(testCase);

        class DurationMetric : PerformanceMetricInfo
        {
            public static readonly DurationMetric Instance = new DurationMetric();

            DurationMetric() : base("Duration", "Duration", PerformanceMetricUnits.Milliseconds)
            {
            }
        }
    }
}