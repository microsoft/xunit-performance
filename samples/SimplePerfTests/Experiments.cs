using Microsoft.Xunit.Performance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
[assembly:Xunit.CollectionBehavior(DisableTestParallelization = true)]
namespace SimplePerfTests
{
    [MeasureInstructionsRetired(1)]
    public static class Experiments
    {
        [Benchmark]
        public static void Empty()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                }
            }
        }

        static int s_i;

        [Benchmark]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        [InlineData(1000000)]
        public static int VolatileRead(int innerIterations)
        {
            int sum = 0;
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterations; i++)
                        sum += Volatile.Read(ref s_i);
                }
            }
            return sum;
        }
    }
}
