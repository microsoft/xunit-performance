using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xunit.Performance;

namespace Microsoft.Xunit.Performance.Api
{
    public sealed class XunitPerformanceHarness : IDisposable
    {
        private string[] _args;

        public XunitPerformanceHarness(string[] args)
        {
            _args = args;

            // Set the run id.
            Configuration.RunId = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss");

            // Set the file log path.
            // TODO: Conditionally set this based on whether we want a csv file written.
            Configuration.FileLogPath = Configuration.RunId + ".csv";
        }

        void IDisposable.Dispose()
        {
            // Close the log when all test cases have completed execution.
            // TODO: This is a hack because we haven't found a way to close the file from within xunit.
            BenchmarkEventSource.Log.Close();

            // Process the results now that we know we're done executing tests.
            ProcessResults();
        }

        public BenchmarkConfiguration Configuration
        {
            get { return BenchmarkConfiguration.Instance; }
        }

        public void RunBenchmarks(string assemblyPath)
        {
            if(string.IsNullOrEmpty(assemblyPath))
            {
                throw new ArgumentNullException(nameof(assemblyPath));
            }

            // Invoke xunit to run benchmarks in the specified assembly.
            XunitRunner runner = new XunitRunner();
            runner.Run(assemblyPath);
        }

        private void ProcessResults()
        {
            CSVMetricReader reader = new CSVMetricReader(Configuration.FileLogPath);

            foreach(string testCaseName in reader.TestCases)
            {
                Console.WriteLine($"\nTest Case: {testCaseName}");
                List<Dictionary<string, double>> iterations = reader.GetValues(testCaseName);
                for(int i=0; i<iterations.Count; i++)
                {
                    Dictionary<string, double> iter = iterations[i];
                    foreach(KeyValuePair<string, double> result in iter)
                    {
                        Console.WriteLine($"Iter={i}; Metric={result.Key}; Value={result.Value}");
                    }
                }
            }
        }
    }
}