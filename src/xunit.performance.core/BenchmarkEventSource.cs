using System;
using System.Diagnostics.Tracing;

namespace Microsoft.Xunit.Performance.Internal
{
    [EventSource(Name = "Microsoft-Xunit-Benchmark", Guid = "A3B447A8-6549-4158-9BAD-76D442A47061")]
    public sealed class BenchmarkEventSource : EventSource
    {
        public class Tasks
        {
            public const EventTask BenchmarkStart = (EventTask)1;
            public const EventTask BenchmarkStop = (EventTask)2;
            public const EventTask BenchmarkIterationStart = (EventTask)3;
            public const EventTask BenchmarkIterationStop = (EventTask)4;
        }

        public static BenchmarkEventSource Log = new BenchmarkEventSource();

        [Event(1, Opcode = EventOpcode.Info, Task = Tasks.BenchmarkStart)]
        public unsafe void BenchmarkStart(string RunId, string BenchmarkName)
        {
            if (IsEnabled())
            {
                if (RunId == null)
                    RunId = "";

                fixed (char* pRunId = RunId)
                fixed (char* pBenchmarkName = BenchmarkName)
                {
                    EventData* data = stackalloc EventData[2];
                    data[0].Size = (RunId.Length + 1) * sizeof(char);
                    data[0].DataPointer = (IntPtr)pRunId;
                    data[1].Size = (BenchmarkName.Length + 1) * 2;
                    data[1].DataPointer = (IntPtr)pBenchmarkName;
                    WriteEventCore(1, 2, data);
                }
            }
        }

        [Event(2, Opcode = EventOpcode.Info, Task = Tasks.BenchmarkStop)]
        public unsafe void BenchmarkStop(string RunId, string BenchmarkName, string StopReason)
        {
            if (IsEnabled())
            {
                if (RunId == null)
                    RunId = "";

                fixed (char* pRunId = RunId)
                fixed (char* pBenchmarkName = BenchmarkName)
                fixed (char* pStopReason = StopReason)
                {
                    EventData* data = stackalloc EventData[3];
                    data[0].Size = (RunId.Length + 1) * sizeof(char);
                    data[0].DataPointer = (IntPtr)pRunId;
                    data[1].Size = (BenchmarkName.Length + 1) * 2;
                    data[1].DataPointer = (IntPtr)pBenchmarkName;
                    data[2].Size = (StopReason.Length + 1) * 2;
                    data[2].DataPointer = (IntPtr)pStopReason;
                    WriteEventCore(2, 3, data);
                }
            }
        }

        [Event(3, Opcode = EventOpcode.Info, Task = Tasks.BenchmarkIterationStart)]
        public unsafe void BenchmarkIterationStart(string RunId, string BenchmarkName, int Iteration)
        {
            if (IsEnabled())
            {
                if (RunId == null)
                    RunId = "";

                fixed (char* pRunId = RunId)
                fixed (char* pBenchmarkName = BenchmarkName)
                {
                    EventData* data = stackalloc EventData[3];
                    data[0].Size = (RunId.Length + 1) * sizeof(char);
                    data[0].DataPointer = (IntPtr)pRunId;
                    data[1].Size = (BenchmarkName.Length + 1) * sizeof(char);
                    data[1].DataPointer = (IntPtr)pBenchmarkName;
                    data[2].Size = sizeof(int);
                    data[2].DataPointer = (IntPtr)(&Iteration);
                    WriteEventCore(3, 3, data);
                }
            }
        }

        [Event(4, Opcode = EventOpcode.Info, Task = Tasks.BenchmarkIterationStop)]
        public unsafe void BenchmarkIterationStop(string RunId, string BenchmarkName, int Iteration, bool Success)
        {
            if (IsEnabled())
            {
                if (RunId == null)
                    RunId = "";

                int successInt = Success ? 1 : 0;
                fixed (char* pRunId = RunId)
                fixed (char* pBenchmarkName = BenchmarkName)
                {
                    EventData* data = stackalloc EventData[4];
                    data[0].Size = (RunId.Length + 1) * sizeof(char);
                    data[0].DataPointer = (IntPtr)pRunId;
                    data[1].Size = (BenchmarkName.Length + 1) * 2;
                    data[1].DataPointer = (IntPtr)pBenchmarkName;
                    data[2].Size = sizeof(int);
                    data[2].DataPointer = (IntPtr)(&Iteration);
                    data[3].Size = sizeof(int);
                    data[3].DataPointer = (IntPtr)(&successInt);
                    WriteEventCore(4, 4, data);
                }
            }
        }
    }
}
