using System;


namespace Microsoft.Xunit.Performance.Api
{
    public class PerformanceTestConfig
    {
        public PerformanceTestConfig() 
        {
            Iterations = 30;
            TimeoutPerIteration = TimeSpan.FromMilliseconds(20000);
        }

        public TimeSpan TimeoutPerIteration 
        {
            get { return _timeoutPerIteration; }

            set
            {
                if (value.TotalMilliseconds > 0) 
                {
                    _timeoutPerIteration = value;
                }
                else 
                {
                    Console.Error.WriteLine("Timeout per iteration should be a positive amount of milliseconds.");
                }
            }
        }
        public int Iterations
        {
            get { return _iterations; }

            set
            {
                if (value > 1) 
                {
                    _iterations = value;
                }
                else 
                {
                    Console.Error.WriteLine("The amount of iterations should be a positive integer greater than 1.");
                }
            }
        }

        private TimeSpan _timeoutPerIteration;
        private int _iterations;
        public string TemporaryDirectory { get; set; }
    }
}