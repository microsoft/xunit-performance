using Microsoft.Xunit.Performance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplePerfTests
{
    public class DictionaryThroughput
    {
        [Benchmark]
        static void Add()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                var dict = new Dictionary<int, string>();

                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 1000; i++)
                        dict.Add(i, "hello");
                }
            }
        }

        [Benchmark]
        static void TestWithSetupAndTeardown()
        {
            // ...any per-testcase setup can go here.

            foreach (var iteration in Benchmark.Iterations)
            {
                // ...per-iteration setup goes here

                using (iteration.StartMeasurement())
                {
                    // ...only code in here is actually measured
                }

                // ...per-iteration cleanup here
            }

            // ...per-testcase cleanup here
        }

        [Benchmark]
        static void SuperSimpleTest()
        {
            Benchmark.Iterate(() => DoSomething());
        }

        [Benchmark]
        static async Task SuperSimpleAsyncTest()
        {
            await Benchmark.IterateAsync(() => DoSomethingAsync());
        }

        private static void DoSomething()
        {
            throw new NotImplementedException();
        }

        private static Task DoSomethingAsync()
        {
            return Task.CompletedTask;
        }
    }
}
