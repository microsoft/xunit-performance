using System;
using System.Diagnostics.Tracing;

namespace Microsoft.Xunit.Performance
{
    [EventSource(Name = "Microsoft-Xunit-Benchmark", Guid = "A3B447A8-6549-4158-9BAD-76D442A47061")]
    internal sealed class BenchmarkEventSource : EventSource
    {
        public class Tasks
        {
            public const EventTask BenchmarkExecution = (EventTask)1;
        }

        public static BenchmarkEventSource Log = new BenchmarkEventSource();

        [Event(1, Opcode = EventOpcode.Info, Task = Tasks.BenchmarkExecution)]
        public unsafe void BenchmarkExecutionStart(string RunId, string BenchmarkName, int Iteration)
        {
            if (IsEnabled())
            {
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
                    WriteEventCore(1, 3, data);
                }
            }
        }

        [Event(2, Opcode = EventOpcode.Info, Task = Tasks.BenchmarkExecution)]
        public unsafe void BenchmarkExecutionStop(string RunId, string BenchmarkName, int Iteration, bool Success)
        {
            if (IsEnabled())
            {
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
                    WriteEventCore(2, 4, data);
                }
            }
        }
    }
}
