


namespace Microsoft.Xunit.Performance.Api
{
    public class PerformanceTestConfig
    {
        public PerformanceTestConfig() 
        {
            iterations = 30;
            timeout = 20000;
        }

        public int iterations;
        public int timeout;
        public string tmpDir{get; set;}
    }
}