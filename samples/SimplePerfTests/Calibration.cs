// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance;
using System;
using System.Threading;
using Xunit;

namespace SimplePerfTests
{
    [MeasureInstructionsRetired(1)]
    public static class Calibration
    {
        [Benchmark]
        public static void Empty()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                }
            }
        }

        static int s_i;

        [Benchmark]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        [InlineData(1000000)]
        public static int BusyWork(int innerIterations)
        {
            int sum = 0;
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterations; i++)
                        sum += Volatile.Read(ref s_i);
                }
            }
            return sum;
        }

        [Benchmark]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public static void Sleep(int milliseconds)
        {
            var ev = new ManualResetEvent(initialState: false);

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    ev.WaitOne(milliseconds);
        }
    }
}
