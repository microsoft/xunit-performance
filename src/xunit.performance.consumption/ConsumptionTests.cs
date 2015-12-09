using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using xunit.performance.consumption;
using Microsoft.Xunit.Performance;

namespace FunctionalTests
{
    [MeasureGCCounts]
    [MeasureGCAllocations]
    public static class ConsumptionTests
    {
        [Fact]
        public static void consumptionTest()
        {
            FormatXML.formatXML(@"D:\PerfUnitTest\xunit-performance\src\xunit.performance.run\bin\Debug\test\telemetry 2015-12-09-00-04-57.xml");
        }

        [Benchmark]
        [InlineData(100)]
        public static void myPerfTest(int iterations)
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < iterations; i++)
                    {
                        using (System.IO.StreamReader a = new System.IO.StreamReader(@"D:\PerfUnitTest\xunit-performance-master\src\xunit.performance.run\bin\Debug\test\2015-12-02-21-28-24.xml"))
                        {

                        }
                    }
                }
            }
        }

    }
}
