// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Xunit.Performance.Api.Profilers.Etw
{
    /// <summary>
    /// Defines a performance monitor sample.
    /// </summary>
    sealed class PmcSample
    {
        /// <summary>
        /// Instruction pointer
        /// </summary>
        public ulong InstructionPointer { get; set; }

        /// <summary>
        /// Process Id associated to this sample.
        /// </summary>
        public int ProcessId { get; set; }

        /// <summary>
        /// Performance monitor counter Id.
        /// </summary>
        public int ProfileSourceId { get; set; }

        /// <summary>
        /// Sampling interval when the performance monitor counter was measured.
        /// </summary>
        public long SamplingInterval { get; set; }

        /// <summary>
        /// DateTime when the event was captured.
        /// </summary>
        public DateTime TimeStamp { get; set; }
    }
}