using Microsoft.Xunit.Performance;

namespace simpleharness
{
    public static class EndToEndTests
    {
        static volatile string s_formatterString;

        [Benchmark(InnerIterationCount = 10000)]
        public static void BenchmarkIterationMeasurementDoubleDispose()
        {
            foreach (BenchmarkIteration iter in Benchmark.Iterations)
            {
                {
                    var benchmarkIterationMeasurement = iter.StartMeasurement();
                    try
                    {
                        for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        {
                            s_formatterString = string.Format("{0}{1}{2}{3}", "a", "b", "c", "d");
                        }
                    }
                    finally
                    {
                        benchmarkIterationMeasurement.Dispose();
                        benchmarkIterationMeasurement.Dispose();
                    }
                }
            }
        }
    }
}