using System;
using System.Reflection;

namespace Microsoft.Xunit.Performance.Execution
{
    /// <summary>
    /// 
    /// </summary>
    internal static class AllocatedBytesForCurrentThread
    {
        /// <summary>
        /// Loads the GetAllocatedBytesForCurrentThread method.
        /// </summary>
        /// <remarks>
        /// Calling `gcGetAllocMethod.Invoke(null, null);` will cause an object
        /// to be returned (it allocates a minimum object), so calling the
        /// method back to back and computing the difference between them will
        /// not be equals to zero (it will differ by 24 bytes).
        /// </remarks>
        static AllocatedBytesForCurrentThread()
        {
            var typeInfo = typeof(GC).GetTypeInfo();
            var method = typeInfo.GetMethod("GetAllocatedBytesForCurrentThread",
                            BindingFlags.Public | BindingFlags.Static)
                      ?? typeInfo.GetMethod("GetAllocatedBytesForCurrentThread",
                            BindingFlags.NonPublic | BindingFlags.Static);

            // TODO: Add meaningful error message.

            _GetAllocatedBytesForCurrentThread = method == null ?
                (Func<long>)(() => 0xBAAAAAAD) : () => (long)method.Invoke(null, null);

            var allocatedBytesBefore = LastAllocatedBytes;
            var allocatedBytesAfter = LastAllocatedBytes;
            s_minAllocatedBytes = allocatedBytesAfter - allocatedBytesBefore;
        }

        public static long LastAllocatedBytes => _GetAllocatedBytesForCurrentThread.Invoke();

        public static long GetTotalAllocatedBytes(long allocatedBytesBefore, long allocatedBytesAfter)
        {
            return allocatedBytesAfter - allocatedBytesBefore; // FIXME: (allocatedBytesAfter - allocatedBytesBefore - s_minAllocatedBytes) sometimes returns a negative value!
        }

        private static readonly Func<long> _GetAllocatedBytesForCurrentThread;
        private static readonly long s_minAllocatedBytes;
    }
}
