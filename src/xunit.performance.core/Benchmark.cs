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
        /// Gets a sequence of benchmark iterations.
        /// </summary>
        /// <remarks>
        /// Call <see cref="BenchmarkIteration.StartMeasurement"/> on each iteration
        /// before executing the code to be measured.
        /// </remarks>
        /// <returns></returns>
        public static IEnumerable<BenchmarkIteration> Iterations => BenchmarkIterator.Current.Iterations;

        /// <summary>
        /// Calls a method for each iteration of the current benchmark.
        /// </summary>
        /// <param name="measuredMethod"></param>
        public static void Iterate(Action measuredMethod)
        {
            foreach (var iteration in Iterations)
                using (iteration.StartMeasurement())
                    measuredMethod();
        }

        /// <summary>
        /// Calls an async method for each iteration of the current benchmark.
        /// </summary>
        /// <param name="measuredMethod"></param>
        /// <returns></returns>
        public static async Task IterateAsync(Func<Task> measuredMethod)
        {
            foreach (var iteration in Iterations)
                using (iteration.StartMeasurement())
                    await measuredMethod();
        }
    }
}