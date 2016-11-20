using System;
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
    }
}