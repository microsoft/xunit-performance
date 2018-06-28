// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance.Internal;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Xunit.Performance
{
    /// <summary>
    /// Provides methods to control benchmark execution.
    /// </summary>
    public static class Benchmark
    {
        /// <summary>
        /// Gets the inner iteration count (the count of actual iterations that should occur for each benchmark iteration).
        /// </summary>
        public static long InnerIterationCount => BenchmarkIterator.Current.InnerIterationCount;

        /// <summary>
        /// Gets a sequence of benchmark iterations.
        /// </summary>
        /// <remarks>
        /// Call <see cref="BenchmarkIteration.StartMeasurement"/> on each iteration
        /// before executing the code to be measured.
        /// </remarks>
        /// <returns></returns>
        public static IEnumerable<BenchmarkIteration> Iterations => BenchmarkIterator.Current.Iterations;

        /// <summary>
        /// Automatically iterate over a measured operation.
        /// </summary>
        /// <param name="measuredOperation">The operation to measure and iterate over.</param>
        public static void Iterate(Action measuredOperation)
        {
            foreach (var iteration in BenchmarkIterator.Current.Iterations)
            {
                using (var measurement = iteration.StartMeasurement())
                {
                    for (int i = 0; i < BenchmarkIterator.Current.InnerIterationCount; i++)
                    {
                        measuredOperation.Invoke();
                    }
                }
            }
        }

        /// <summary>
        /// Automatically iterate over an asynchronous measured operation.
        /// </summary>
        /// <param name="asyncMeasuredOperation">The operation to measure and iterate over.</param>
        public static async Task IterateAsync(Func<Task> asyncMeasuredOperation)
        {
            foreach (var iteration in BenchmarkIterator.Current.Iterations)
            {
                using (var measurement = iteration.StartMeasurement())
                {
                    for (int i = 0; i < BenchmarkIterator.Current.InnerIterationCount; i++)
                    {
                        await asyncMeasuredOperation.Invoke();
                    }
                }
            }
        }
    }
}