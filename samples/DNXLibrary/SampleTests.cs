using Microsoft.Xunit.Performance;
using Xunit;

namespace DNXLibrary
{
    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
    public class DNXSampleTests
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
            Benchmark.Iterate(() => { });
        }

        [Benchmark]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void Benchmark2(int x)
        {
            Benchmark.Iterate(() => { });
        }
    }
}