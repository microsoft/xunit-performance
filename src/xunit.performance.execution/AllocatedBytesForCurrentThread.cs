using System;
using System.Reflection;

namespace Microsoft.Xunit.Performance.Execution
{
    /// <summary>
    ///
    /// </summary>
    static class AllocatedBytesForCurrentThread
    {
        static readonly Func<long> _GetAllocatedBytesForCurrentThread;

        static readonly long s_minAllocatedBytes;

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
            var method = typeof(GC)
                .GetTypeInfo()
                .GetMethod("GetAllocatedBytesForCurrentThread", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (method == null)
            {
                _GetAllocatedBytesForCurrentThread = () => -0xBAAAAAAD;
                IsAvailable = false;
                NoAvailabilityReason = "The running implementation of .NET does not expose 'GC.GetAllocatedBytesForCurrentThread'.";
            }
            else
            {
                _GetAllocatedBytesForCurrentThread = () => (long)method.Invoke(null, null);
                if (IsBugFixed())
                {
                    IsAvailable = true;
                    NoAvailabilityReason = "";
                }
                else
                {
                    IsAvailable = false;
                    NoAvailabilityReason = "Not capturing 'GC.GetAllocatedBytesForCurrentThread' because the targeted runtime does not contain the fix for 'https://github.com/dotnet/coreclr/issues/10207'";
                }
            }

            var allocatedBytesBefore = LastAllocatedBytes;
            var allocatedBytesAfter = LastAllocatedBytes;
            s_minAllocatedBytes = allocatedBytesAfter - allocatedBytesBefore;
        }

        public static bool IsAvailable { get; }

        public static long LastAllocatedBytes => _GetAllocatedBytesForCurrentThread.Invoke();

        public static string NoAvailabilityReason { get; }

        public static long GetTotalAllocatedBytes(long allocatedBytesBefore, long allocatedBytesAfter) => (allocatedBytesAfter - allocatedBytesBefore - s_minAllocatedBytes);

        static bool IsBugFixed()
        {
            GC.Collect();
            var allocatedBytesBeforeTest = LastAllocatedBytes;
            GC.Collect();
            var allocatedBytesAfterTest = LastAllocatedBytes;

            return (allocatedBytesAfterTest - allocatedBytesBeforeTest == 24);
        }
    }
}