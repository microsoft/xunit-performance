// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance.Execution;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.Xunit.Performance
{
    [EventSource(Name = "Microsoft-Xunit-Benchmark")]
    public sealed class BenchmarkEventSource : EventSource
    {
        public static class Tasks
        {
            public const EventTask Benchmark = (EventTask)1;
            public const EventTask BenchmarkIteration = (EventTask)2;
        }

        [NonEvent]
        private static StreamWriter OpenCSV()
        {
            var logPath = BenchmarkConfiguration.Instance.FileLogPath;
            if (logPath == null)
                return null;

            var fs = new FileStream(logPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            return new StreamWriter(fs, Encoding.UTF8);
        }

        [NonEvent]
        internal void Flush()
        {
            _csvWriter?.Flush();
        }

        [NonEvent]
        public void Clear()
        {
            _csvWriter?.BaseStream.SetLength(0);
            _csvWriter?.BaseStream.Flush();
        }

        // This can only be called when the process is done using the EventSource
        // and all test cases have completed running.
        // TODO: Remove once the CSV functionality is no longer needed.
        [NonEvent]
        public void Close()
        {
            _csvWriter?.Dispose();
        }

        [NonEvent]
        private double GetTimestamp()
        {
            return (1000.0 * Stopwatch.GetTimestamp()) / Stopwatch.Frequency;
        }

        [NonEvent]
        private static string Escape(string s)
        {
            return s.Replace(@"\", @"\\").Replace(",", @"\_").Replace("\n", @"\n").Replace("\r", @"\r");
        }

        [NonEvent]
        private void WriteCSV(
            string benchmarkName,
            [CallerMemberName]string eventName = null,
            string stopReason = "",
            int? iteration = null,
            bool? success = null)
        {
            // TODO: this is going to add a lot of overhead; it's just here to get us running while we wait for an ETW-equivalent on Linux.
            _csvWriter.WriteLine($"{GetTimestamp()},{Escape(benchmarkName)},{eventName},{iteration?.ToString(CultureInfo.InvariantCulture) ?? ""},{success?.ToString() ?? ""},{stopReason}");
        }

        [Event(1, Opcode = EventOpcode.Start, Task = Tasks.Benchmark)]
        public unsafe void BenchmarkStart(string RunId, string BenchmarkName)
        {
            if (_csvWriter != null)
                WriteCSV(BenchmarkName);

            if (IsEnabled())
            {
                if (RunId == null)
                    RunId = DefaultRunId;

                fixed (char* pRunId = RunId)
                fixed (char* pBenchmarkName = BenchmarkName)
                {
                    const int eventDataCount = 2;
                    EventData* data = stackalloc EventData[eventDataCount];
                    data[0].Size = (RunId.Length + 1) * sizeof(char);
                    data[0].DataPointer = (IntPtr)pRunId;
                    data[1].Size = (BenchmarkName.Length + 1) * sizeof(char);
                    data[1].DataPointer = (IntPtr)pBenchmarkName;
                    WriteEventCore(1, eventDataCount, data);
                }
            }
        }

        [Event(2, Opcode = EventOpcode.Stop, Task = Tasks.Benchmark)]
        public unsafe void BenchmarkStop(string RunId, string BenchmarkName, string StopReason)
        {
            if (IsEnabled())
            {
                if (RunId == null)
                    RunId = DefaultRunId;

                fixed (char* pRunId = RunId)
                fixed (char* pBenchmarkName = BenchmarkName)
                fixed (char* pStopReason = StopReason)
                {
                    const int eventDataCount = 3;
                    EventData* data = stackalloc EventData[eventDataCount];
                    data[0].Size = (RunId.Length + 1) * sizeof(char);
                    data[0].DataPointer = (IntPtr)pRunId;
                    data[1].Size = (BenchmarkName.Length + 1) * sizeof(char);
                    data[1].DataPointer = (IntPtr)pBenchmarkName;
                    data[2].Size = (StopReason.Length + 1) * 2;
                    data[2].DataPointer = (IntPtr)pStopReason;
                    WriteEventCore(2, eventDataCount, data);
                }
            }

            if (_csvWriter != null)
                WriteCSV(BenchmarkName, stopReason: StopReason);
        }

        [Event(3, Level = EventLevel.LogAlways, Opcode = EventOpcode.Start, Task = Tasks.BenchmarkIteration)]
        public unsafe void BenchmarkIterationStart(string RunId, string BenchmarkName, int Iteration)
        {
            if (_csvWriter != null)
                WriteCSV(BenchmarkName, iteration: Iteration);

            if (IsEnabled())
            {
                if (RunId == null)
                    RunId = DefaultRunId;

                // Capture the allocated bytes just before writing an event.
                _allocatedBytes = AllocatedBytesForCurrentThread.LastAllocatedBytes;

                fixed (char* pRunId = RunId)
                fixed (char* pBenchmarkName = BenchmarkName)
                {
                    const int eventDataCount = 3;
                    EventData* data = stackalloc EventData[eventDataCount];
                    data[0].Size = (RunId.Length + 1) * sizeof(char);
                    data[0].DataPointer = (IntPtr)pRunId;
                    data[1].Size = (BenchmarkName.Length + 1) * sizeof(char);
                    data[1].DataPointer = (IntPtr)pBenchmarkName;
                    data[2].Size = sizeof(int);
                    data[2].DataPointer = (IntPtr)(&Iteration);

                    WriteEventCore(3, eventDataCount, data);
                }
            }
        }

        [Event(4, Level = EventLevel.LogAlways, Opcode = EventOpcode.Stop, Task = Tasks.BenchmarkIteration)]
        public unsafe void BenchmarkIterationStop(string RunId, string BenchmarkName, int Iteration, long AllocatedBytes = 0 /*dummy value for the TraceParserGen tool*/)
        {
            if (IsEnabled())
            {
                if (RunId == null)
                    RunId = DefaultRunId;

                var allocatedBytes = AllocatedBytesForCurrentThread.LastAllocatedBytes;
                allocatedBytes = AllocatedBytesForCurrentThread.GetTotalAllocatedBytes(
                    _allocatedBytes, allocatedBytes);
                fixed (char* pRunId = RunId)
                fixed (char* pBenchmarkName = BenchmarkName)
                {
                    const int eventDataCount = 4;
                    EventData* data = stackalloc EventData[eventDataCount];
                    data[0].Size = (RunId.Length + 1) * sizeof(char);
                    data[0].DataPointer = (IntPtr)pRunId;
                    data[1].Size = (BenchmarkName.Length + 1) * sizeof(char);
                    data[1].DataPointer = (IntPtr)pBenchmarkName;
                    data[2].Size = sizeof(int);
                    data[2].DataPointer = (IntPtr)(&Iteration);
                    data[3].Size = sizeof(long);
                    data[3].DataPointer = (IntPtr)(&allocatedBytes);

                    WriteEventCore(4, eventDataCount, data);
                }
            }

            if (_csvWriter != null)
                WriteCSV(BenchmarkName, iteration: Iteration);
        }

        public static readonly BenchmarkEventSource Log = new BenchmarkEventSource();

        private const string DefaultRunId = "";
        private static readonly StreamWriter _csvWriter = OpenCSV();
        private static long _allocatedBytes = 0;
    }
}
