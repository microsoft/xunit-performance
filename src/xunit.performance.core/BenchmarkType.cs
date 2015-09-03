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
    public enum BenchmarkType
    {
        /// <summary>
        /// Indicates that the method is a simple benchmark, with iterations controlled by the xunit.performance framework.
        /// </summary>
        Simple,

        /// <summary>
        /// Indicates that the method requires manual control over iterations.
        /// </summary>
        /// <remarks>
        /// The method's first argument must be of type <see cref="BenchmarkIterationControl"/>.
        /// </remarks>
        ManualControl
    }
}
