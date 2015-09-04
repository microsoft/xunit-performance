using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Xunit.Performance.Internal
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class BenchmarkIterator
    {
        internal static BenchmarkIterator Current { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public async Task RunAsync(Func<Task> method)
        {
            if (Current != null)
                throw new InvalidOperationException();

            Current = this;
            try
            {
                await method();
            }
            finally
            {
                Current = null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected internal abstract IEnumerable<BenchmarkIteration> Iterations { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iterationNumber"></param>
        protected internal abstract void StartMeasurement(int iterationNumber);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iterationNumber"></param>
        protected internal abstract void StopMeasurement(int iterationNumber);

        protected BenchmarkIteration CreateIteration(int iterationNumber) => new BenchmarkIteration(this, iterationNumber);
    }
}
