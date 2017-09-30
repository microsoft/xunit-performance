// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Xunit.Performance.Api.Profilers.Etw
{
    internal sealed class EtwPerfInfoPMCSample
    {
        public int ProcessId { get; set; }

        public ulong InstructionPointer { get; set; }

        public int ProfileSource { get; set; }

        public int ThreadId { get; set; }

        public DateTime TimeStamp { get; set; }
    }
}
