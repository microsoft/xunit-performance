using Microsoft.Xunit.Performance;
using System.Threading;
using Xunit;

namespace DNXLibrary
{
    public class DNXSampleTests
    {
        [Benchmark]
        public void DoNothing()
        {
            Benchmark.Iterate(() => { });
        }

        [Benchmark]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void Spin(int x)
        {
            Benchmark.Iterate(() => 
            {
                for (var spin = new SpinWait(); spin.Count < x; spin.SpinOnce())
                {

                }
            });
        }
    }
}