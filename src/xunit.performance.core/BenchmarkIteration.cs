// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance.Internal;

namespace Microsoft.Xunit.Performance
{
    /// <summary>
    /// Represents a single iteration of a benchmark.
    /// </summary>
    public struct BenchmarkIteration
    {
        readonly int _iterationNumber;
        readonly BenchmarkIterator _iterator;

        internal BenchmarkIteration(BenchmarkIterator iterator, int iterationNumber)
        {
            _iterator = iterator;
            _iterationNumber = iterationNumber;
        }

        /// <summary>
        /// Starts measuring performance metrics for this iteration.
        /// </summary>
        /// <returns></returns>
        public BenchmarkIterationMeasurement StartMeasurement()
        {
            _iterator.StartMeasurement(_iterationNumber);
            return new BenchmarkIterationMeasurement(this);
        }

        internal void StopMeasurement() => _iterator.StopMeasurement(_iterationNumber);
    }
}