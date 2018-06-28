using Microsoft.Xunit.Performance;

namespace simpleharness
{
    public static class EmptyBenchmarkTest
    {
        [Benchmark]
        public static void Implementation_1()
        {
            foreach (BenchmarkIteration iter in Benchmark.Iterations)
            {
                using (iter.StartMeasurement())
                {
                }
            }
        }

        [Benchmark]
        public static void Implementation_2() => Benchmark.Iterate(() => { /*do nothing*/ });
    }
}