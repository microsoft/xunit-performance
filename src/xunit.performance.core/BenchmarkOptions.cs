using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Xunit.Performance
{
    /// <summary>
    /// Controls the execution of a method marked with <see cref="BenchmarkAttribute"/>.
    /// </summary>
    [Flags]
    public enum BenchmarkOptions
    {
        /// <summary>
        /// Indicates that the method requires manual control over iterations.
        /// </summary>
        /// <remarks>
        /// The method's first argument must be of type <see cref="BenchmarkIterationControl"/>.
        /// </remarks>
        ManualControl = 0x1,
    }
}
