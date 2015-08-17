using System;

namespace Microsoft.Xunit.Performance
{
    /// <summary>
    /// Allows specification of whether a [Benchmark] method allocates new objects from the GC heap.  
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class AllocatesAttribute : Attribute
    {
        public AllocatesAttribute(bool allocates) { Allocates = allocates; }

        public bool Allocates { get; private set; }
    }
}
