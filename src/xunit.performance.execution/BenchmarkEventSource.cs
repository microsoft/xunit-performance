// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.Xunit.Performance
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

        public static readonly BenchmarkEventSource Log = new BenchmarkEventSource();

        private static readonly StreamWriter _csvWriter = OpenCSV();

        private static StreamWriter OpenCSV()
        {
            var logPath = BenchmarkConfiguration.FileLogPath;
            if (logPath == null)
                return null;

            return new StreamWriter(File.Open(logPath, FileMode.Create), encoding: Encoding.UTF8);
        }

        internal void Flush()
        {
            if (_csvWriter != null)
                _csvWriter.Flush();
        }

        private double GetTimestamp()
        {
            return 1000.0 * (double)Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
        }

        private void WriteCSV(
            string runId, 
            string benchmarkName, 
            [CallerMemberName]string eventName = null,
            string stopReason = "",
            int? iteration = null,
            bool? success = null)
        {
            // TODO: this is going to add a lot of overhead; it's just here to get us running while we wait for an ETW-equivalent on Linux.
            _csvWriter.WriteLine($"{GetTimestamp()},{runId},{benchmarkName},{eventName},{stopReason},{iteration?.ToString(CultureInfo.InvariantCulture) ?? ""},{success?.ToString() ?? ""}");
        }

        [Event(1, Opcode = EventOpcode.Info, Task = Tasks.BenchmarkStart)]
        public unsafe void BenchmarkStart(string RunId, string BenchmarkName)
        {
            if (_csvWriter != null)
                WriteCSV(RunId, BenchmarkName);

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

            if (_csvWriter != null)
                WriteCSV(RunId, BenchmarkName, stopReason: StopReason);
        }

        [Event(3, Opcode = EventOpcode.Info, Task = Tasks.BenchmarkIterationStart)]
        public unsafe void BenchmarkIterationStart(string RunId, string BenchmarkName, int Iteration)
        {
            if (_csvWriter != null)
                WriteCSV(RunId, BenchmarkName, iteration: Iteration);

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

            if (_csvWriter != null)
                WriteCSV(RunId, BenchmarkName, iteration: Iteration, success: Success);
        }
    }
}
