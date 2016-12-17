using Microsoft.Xunit.Performance;
using Microsoft.Xunit.Performance.Api;
using System.Reflection;

[assembly: MeasureGCAllocations]
[assembly: MeasureGCCounts]
[assembly: MeasureInstructionsRetired]
[assembly: OptimizeForBenchmarks]

namespace simpleharness
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using (var p = new XunitPerformanceHarness(args))
                p.RunBenchmarks(Assembly.GetEntryAssembly().Location);
        }

        public sealed class Type_1
        {
            [Benchmark(InnerIterationCount = 10000)]
            public void TestBenchmark()
            {
                foreach (BenchmarkIteration iter in Benchmark.Iterations)
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

        public sealed class Type_2
        {
            [Benchmark(InnerIterationCount = 10000)]
            public void TestBenchmark1()
            {
                foreach (BenchmarkIteration iter in Benchmark.Iterations)
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
            public void TestBenchmark2()
            {
                foreach (BenchmarkIteration iter in Benchmark.Iterations)
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
}