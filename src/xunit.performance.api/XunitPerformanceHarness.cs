using System;
using System.Reflection;

namespace Microsoft.Xunit.Performance.Api
{
    public sealed class XunitPerformanceHarness : IDisposable
    {
        private string[] _args;

        public XunitPerformanceHarness(string[] args)
        {
            _args = args;
        }

        void IDisposable.Dispose()
        {
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