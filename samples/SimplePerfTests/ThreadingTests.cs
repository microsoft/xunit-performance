using Microsoft.Xunit.Performance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SimplePerfTests
{
    public class ThreadingTests
    {
        struct ThreadPoolConfiguration : IDisposable
        {
            public int minWorkerThreads;
            public int minCompletionPortThreads;
            public int maxWorkerThreads;
            public int maxCompletionPortThreads;

            public void Dispose()
            {
                // set each value twice, so that we won't fail in the case where the new min is greater than the old max, etc.
                ThreadPool.SetMinThreads(minWorkerThreads, minCompletionPortThreads);
                ThreadPool.SetMaxThreads(maxWorkerThreads, maxCompletionPortThreads);
                Assert.True(ThreadPool.SetMinThreads(minWorkerThreads, minCompletionPortThreads));
                Assert.True(ThreadPool.SetMaxThreads(maxWorkerThreads, maxCompletionPortThreads));
            }
        }

        static ThreadPoolConfiguration ConfigureThreadPool(int minWorkerThreads = 0, int minCompletionPortThreads = 0, int maxWorkerThreads = 0, int maxCompletionPortThreads = 0)
        {
            var oldConfig = new ThreadPoolConfiguration();
            ThreadPool.GetMinThreads(out oldConfig.minWorkerThreads, out oldConfig.minCompletionPortThreads);
            ThreadPool.GetMaxThreads(out oldConfig.maxWorkerThreads, out oldConfig.maxCompletionPortThreads);

            var newConfig = oldConfig;
            if (minWorkerThreads != 0)
            {
                Assert.True(maxWorkerThreads == 0 || maxWorkerThreads >= minWorkerThreads);
                newConfig.minWorkerThreads = minWorkerThreads;
                if (newConfig.maxWorkerThreads < newConfig.minWorkerThreads)
                    newConfig.maxWorkerThreads = newConfig.minWorkerThreads;
            }
            if (minCompletionPortThreads != 0)
            {
                Assert.True(maxCompletionPortThreads == 0 || maxCompletionPortThreads >= minCompletionPortThreads);
                newConfig.minCompletionPortThreads = minCompletionPortThreads;
                if (newConfig.maxCompletionPortThreads < newConfig.minCompletionPortThreads)
                    newConfig.maxCompletionPortThreads = newConfig.minCompletionPortThreads;
            }
            if (maxWorkerThreads != 0)
            {
                Assert.True(minWorkerThreads == 0 || minWorkerThreads <= maxWorkerThreads);
                newConfig.maxWorkerThreads = maxWorkerThreads;
                if (newConfig.minWorkerThreads > newConfig.maxWorkerThreads)
                    newConfig.minWorkerThreads = newConfig.maxWorkerThreads;
            }
            if (maxCompletionPortThreads != 0)
            {
                Assert.True(minCompletionPortThreads == 0 || minCompletionPortThreads <= maxCompletionPortThreads);
                newConfig.maxCompletionPortThreads = maxCompletionPortThreads;
                if (newConfig.minCompletionPortThreads > newConfig.maxCompletionPortThreads)
                    newConfig.minCompletionPortThreads = newConfig.maxCompletionPortThreads;
            }

            // set each value twice, so that we won't fail in the case where the new min is greater than the old max, etc.
            ThreadPool.SetMinThreads(newConfig.minWorkerThreads, newConfig.minCompletionPortThreads);
            ThreadPool.SetMaxThreads(newConfig.maxWorkerThreads, newConfig.maxCompletionPortThreads);
            Assert.True(ThreadPool.SetMinThreads(newConfig.minWorkerThreads, newConfig.minCompletionPortThreads));
            Assert.True(ThreadPool.SetMaxThreads(newConfig.maxWorkerThreads, newConfig.maxCompletionPortThreads));

            return oldConfig;
        }

        public static IEnumerable<object[]> ThreadPoolThroughputData
        {
            get
            {
                for (int nThreads = 1; nThreads <= 1024; nThreads *= 2)
                    for (int nTasks = 1; nTasks <= 1024; nTasks *= 2)
                        yield return new object[] { nThreads, nTasks };
            }
        }

        [Benchmark]
        [MemberData(nameof(ThreadPoolThroughputData))]
        public static async Task ThreadPoolThroughput(int nThreads, int nTasks)
        {
            using (ConfigureThreadPool(minWorkerThreads: nThreads, maxWorkerThreads: nThreads))
            {
                foreach (var iteration in Benchmark.Iterations)
                {
                    var tasks = new Task[nTasks];

                    using (iteration.StartMeasurement())
                    {
                        for (int i = 0; i < tasks.Length; i++)
                            tasks[i] = Task.Run(() => { });

                        await Task.WhenAll(tasks);
                    }
                }
            }
        }
    }
}
