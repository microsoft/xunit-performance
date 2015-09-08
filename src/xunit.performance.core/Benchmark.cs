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
    }
}