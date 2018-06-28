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
using static Microsoft.Xunit.Performance.Api.PerformanceLogger;

namespace Microsoft.Xunit.Performance
{
    [EventSource(Name = "Microsoft-Xunit-Benchmark")]
    public sealed class BenchmarkEventSource : EventSource
    {
        const string DefaultRunId = "";

        static readonly StreamWriter s_csvWriter;

        static long s_allocatedBytes;

        static BenchmarkEventSource()
        {
            Log = new BenchmarkEventSource();
            s_csvWriter = OpenCSV();
            s_allocatedBytes = 0;
        }

        BenchmarkEventSource() : base(true)
        {
        }

        public static BenchmarkEventSource Log { get; private set; }

        [Event(3, Level = EventLevel.LogAlways, Opcode = EventOpcode.Start, Task = Tasks.BenchmarkIteration)]
        public unsafe void BenchmarkIterationStart(string RunId, string BenchmarkName, int Iteration)
        {
            if (s_csvWriter != null)
                WriteCSV(BenchmarkName, iteration: Iteration);

            if (IsEnabled())
            {
                if (RunId == null)
                    RunId = DefaultRunId;

                // Capture the allocated bytes just before writing an event.
                s_allocatedBytes = AllocatedBytesForCurrentThread.LastAllocatedBytes;

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
                    s_allocatedBytes, allocatedBytes);
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

            if (s_csvWriter != null)
                WriteCSV(BenchmarkName, iteration: Iteration);
        }

        [Event(1, Level = EventLevel.LogAlways, Opcode = EventOpcode.Start, Task = Tasks.Benchmark)]
        public unsafe void BenchmarkStart(string RunId, string BenchmarkName)
        {
            if (s_csvWriter != null)
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

        [Event(2, Level = EventLevel.LogAlways, Opcode = EventOpcode.Stop, Task = Tasks.Benchmark)]
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

            if (s_csvWriter != null)
                WriteCSV(BenchmarkName, stopReason: StopReason);
        }

        [NonEvent]
        public void Clear()
        {
            s_csvWriter?.BaseStream.SetLength(0);
            s_csvWriter?.BaseStream.Flush();
        }

        // This can only be called when the process is done using the EventSource
        // and all test cases have completed running.
        // TODO: Remove once the CSV functionality is no longer needed.
        [NonEvent]
        public void Close() => s_csvWriter?.Dispose();

        [NonEvent]
        internal void Flush() => s_csvWriter?.Flush();

        [NonEvent]
        static string Escape(string s) => s.Replace(@"\", @"\\").Replace(",", @"\_").Replace("\n", @"\n").Replace("\r", @"\r");

        [NonEvent]
        static StreamWriter OpenCSV()
        {
            var logPath = BenchmarkConfiguration.Instance.FileLogPath;
            if (logPath == null)
                return null;

            var fs = new FileStream(logPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            return new StreamWriter(fs, Encoding.UTF8);
        }

        [NonEvent]
        double GetTimestamp() => (1000.0 * Stopwatch.GetTimestamp()) / Stopwatch.Frequency;

        [Conditional("DEBUG")]
        void PrintDebugInformation()
        {
            if (!IsEnabled())
                WriteWarningLine("EventSource is disabled.");
        }

        [NonEvent]
        void WriteCSV(
            string benchmarkName,
            [CallerMemberName]string eventName = null,
            string stopReason = "",
            int? iteration = null,
            bool? success = null) =>
            // TODO: this is going to add a lot of overhead; it's just here to get us running while we wait for an ETW-equivalent on Linux.
            s_csvWriter.WriteLine($"{GetTimestamp().ToString(CultureInfo.InvariantCulture)},{Escape(benchmarkName)},{eventName},{iteration?.ToString(CultureInfo.InvariantCulture) ?? ""},{success?.ToString() ?? ""},{stopReason}");

        public static class Tasks
        {
            public const EventTask Benchmark = (EventTask)1;
            public const EventTask BenchmarkIteration = (EventTask)2;
        }
    }
}