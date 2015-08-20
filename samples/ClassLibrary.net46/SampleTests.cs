using Microsoft.Xunit.Performance;
using Xunit;

namespace ClassLibrary.net46
{
    public class SampleTests
    {
        [Fact]
        public void AlwaysPass()
        {
            Assert.True(true);
        }

        [Fact]
        public void AlwaysFail()
        {
            Assert.True(false);
        }

        [Benchmark]
        public void Benchmark1()
        {
        }

        [Benchmark]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void Benchmark2(int x)
        {
        }
    }
}
