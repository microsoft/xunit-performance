using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;

namespace Microsoft.Xunit.Performance
{
    static class ETWLogging
    {
        const KernelTraceEventParser.Keywords KernelKeywords =
            KernelTraceEventParser.Keywords.Profile |
            KernelTraceEventParser.Keywords.ImageLoad |
            KernelTraceEventParser.Keywords.Process |
            KernelTraceEventParser.Keywords.Thread |
            KernelTraceEventParser.Keywords.MemoryHardFaults |
            KernelTraceEventParser.Keywords.Memory |
            KernelTraceEventParser.Keywords.VAMap |
            KernelTraceEventParser.Keywords.VirtualAlloc |
            //KernelTraceEventParser.Keywords.ContextSwitch |
            KernelTraceEventParser.Keywords.DiskIO |
            KernelTraceEventParser.Keywords.FileIO |
            KernelTraceEventParser.Keywords.FileIOInit;

        const KernelTraceEventParser.Keywords KernelStackKeywords =
            KernelTraceEventParser.Keywords.Profile |
            //KernelTraceEventParser.Keywords.ContextSwitch |
            KernelTraceEventParser.Keywords.FileIO |
            KernelTraceEventParser.Keywords.ImageLoad |
            KernelTraceEventParser.Keywords.VAMap |
            KernelTraceEventParser.Keywords.VirtualAlloc;

        const ClrTraceEventParser.Keywords ClrKeywords =
            ClrTraceEventParser.Keywords.Jit |
            ClrTraceEventParser.Keywords.JittedMethodILToNativeMap |
            ClrTraceEventParser.Keywords.Loader |
            ClrTraceEventParser.Keywords.Exception |
            ClrTraceEventParser.Keywords.Stack |
            ClrTraceEventParser.Keywords.GC;

        const ClrTraceEventParser.Keywords ClrRundownKeywords =
            ClrTraceEventParser.Keywords.Jit |
            ClrTraceEventParser.Keywords.JittedMethodILToNativeMap |
            ClrTraceEventParser.Keywords.Loader |
            ClrTraceEventParser.Keywords.StartEnumeration;

        public static bool CanLog => TraceEventSession.IsElevated() ?? false;

        public static IDisposable Start(string filename, string runId)
        {
            var sessionName = "xunit.performance/" + runId;

            var session = new TraceEventSession(sessionName, filename);
                            
            session.EnableKernelProvider(KernelKeywords, KernelStackKeywords);
            session.EnableProvider(EventSource.GetGuid(typeof(BenchmarkEventSource)));
            session.EnableProvider(ClrTraceEventParser.ProviderGuid, TraceEventLevel.Informational, (ulong)ClrKeywords);
            session.EnableProvider(ClrRundownTraceEventParser.ProviderGuid, TraceEventLevel.Informational, (ulong)ClrRundownKeywords);

            //TODO: add CPU counters, see TraceEventProfileSources

            return session;
        }
    }
}
