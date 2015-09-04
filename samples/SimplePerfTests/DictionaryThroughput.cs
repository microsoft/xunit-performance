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
    }
}
