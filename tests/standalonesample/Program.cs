using Microsoft.Xunit.Performance;
using Microsoft.Xunit.Performance.Api;
using System.Reflection;

namespace standalonesample
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            using (var p = new XunitPerformanceHarness(args))
            {
                string entryAssemblyPath = Assembly.GetEntryAssembly().Location;
                p.RunBenchmarks(entryAssemblyPath);
            }
        }

        [Benchmark(InnerIterationCount = 10000)]
        public static void TestBenchmark()
        {
            foreach (var iter in Benchmark.Iterations)
            {
                using (iter.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        string.Format("{0}{1}{2}{3}", "a", "b", "c", "d");
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 10000)]
        public static void TestBenchmark1()
        {
            foreach (var iter in Benchmark.Iterations)
            {
                using (iter.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        string.Format("{0}{1}{2}{3}", "a", "b", "c", "d");
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 10000)]
        public static void TestBenchmark2()
        {
            foreach (var iter in Benchmark.Iterations)
            {
                using (iter.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        string.Format("{0}{1}{2}{3}", "a", "b", "c", "d");
                    }
                }
            }
        }
    }
}