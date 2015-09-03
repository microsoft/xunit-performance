// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;

namespace Microsoft.Xunit.Performance
{
    /// <summary>
    /// Allows a performance test to manually control test iterations.
    /// </summary>
    public abstract class BenchmarkIterationControl
    {
        /// <summary>
        /// Returns true if the test should execute at least one more iteration.
        /// </summary>
        public abstract bool NeedMoreIterations { get; }

        /// <summary>
        /// Provides a <see cref="IterationCancellationToken"/> that will enter the cancelled state when no more iterations are needed.
        /// </summary>
        public abstract CancellationToken IterationCancellationToken { get; }

        /// <summary>
        /// Called by the test method to indicate that an iteration is starting.
        /// </summary>
        /// <remarks>
        /// When the iteration is complete, call <see cref="RunningBenchmarkIteration.Dispose"/>.
        /// </remarks>
        /// <returns>A <see cref="RunningBenchmarkIteration"/> representing this iteration.</returns>
        public abstract RunningBenchmarkIteration StartNextIteration();

        /// <summary>
        /// Creates a new RunningBenchmarkIteration instance associated with this object.
        /// </summary>
        /// <returns></returns>
        protected RunningBenchmarkIteration CreateRunningBenchmarkIteration()
        {
            return new RunningBenchmarkIteration(this);
        }

        /// <summary>
        /// Called when the current iteration has completed.
        /// </summary>
        protected internal abstract void EndCurrentIteration();

        /// <summary>
        /// Represents a benchmark iteration that is currently executing.
        /// </summary>
        /// <remarks>
        /// Call <see cref="Dispose"/> when the iteration is complete.
        /// </remarks>
        public struct RunningBenchmarkIteration : IDisposable
        {
            private BenchmarkIterationControl _control;

            internal RunningBenchmarkIteration(BenchmarkIterationControl control)
            {
                _control = control;
            }

            /// <summary>
            /// Marks the end of the iteration.
            /// </summary>
            public void Dispose()
            {
                if (_control != null)
                    _control.EndCurrentIteration();
            }
        }
    }
}
