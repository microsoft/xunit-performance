using System;

namespace Microsoft.Xunit.Performance
{
    /// <summary>
    /// Allows specification of whether a [Benchmark] method allocates new objects from the GC heap.  
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class AllocatesAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AllocatesAttribute"/> class.
        /// </summary>
        /// <param name="allocates">True if the test allocates from the GC heap, otherwise false.</param>
        public AllocatesAttribute(bool allocates) { Allocates = allocates; }

        /// <summary>
        /// Indicates whether the test to which this attribute is applied allocates objects from the GC heap.
        /// </summary>
        public bool Allocates { get; private set; }
    }
}
