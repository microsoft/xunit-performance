namespace simpleharness
{
    public class GetAllocatedBytesForCurrentThreadTest
    {
#if NETCOREAPP1_1 || NETCOREAPP2_0
        [Benchmark]
        public static void WithCollect()
        {
            var method = typeof(GC)
                .GetTypeInfo()
                .GetMethod("GetAllocatedBytesForCurrentThread", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            var nBytesBefore = (long)method.Invoke(null, null);
            var nBytesAfter = (long)method.Invoke(null, null);
            var nBytesExtra = nBytesAfter - nBytesBefore;

            foreach (BenchmarkIteration iter in Benchmark.Iterations)
            {
                using (iter.StartMeasurement())
                {
                    for (int i = 0; i < 1000; ++i)
                    {
                        GC.Collect();
                        nBytesBefore = (long)method.Invoke(null, null);
                        GC.Collect();
                        nBytesAfter = (long)method.Invoke(null, null);

                        var totalAllocatedBytes = nBytesAfter - (nBytesBefore + nBytesExtra);
                        if (totalAllocatedBytes != 0)
                            throw new Exception($"Unexpected number of bytes. [{i}]: {totalAllocatedBytes} bytes");
                    }
                }
            }
        }

        [Benchmark]
        public static void WithoutCollect()
        {
            var method = typeof(GC)
                .GetTypeInfo()
                .GetMethod("GetAllocatedBytesForCurrentThread", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            var nBytesBefore = (long)method.Invoke(null, null);
            var nBytesAfter = (long)method.Invoke(null, null);
            var nBytesExtra = nBytesAfter - nBytesBefore;

            Benchmark.Iterate(() =>
            {
                for (int i = 0; i < 1000; ++i)
                {
                    nBytesBefore = (long)method.Invoke(null, null);
                    nBytesAfter = (long)method.Invoke(null, null);

                    var totalAllocatedBytes = nBytesAfter - (nBytesBefore + nBytesExtra);
                    if (totalAllocatedBytes != 0)
                        throw new Exception($"Unexpected number of bytes. [{i}]: {totalAllocatedBytes} bytes");
                }
            });
        }
#endif
    }
}