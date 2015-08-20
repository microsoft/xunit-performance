using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.ProcessDomain;
using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;

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

        static readonly Guid BenchmarkEventSourceGuid = Guid.Parse("A3B447A8-6549-4158-9BAD-76D442A47061");

        public static ProcDomain _loggerDomain = ProcDomain.CreateDomain("Logger", "xunit.performance.logger.exe", runElevated: true);

        private class Stopper : IDisposable
        {
            private string _session;
            public Stopper(string session) { _session = session; }
            public void Dispose()
            {
                _loggerDomain.ExecuteAsync(() => Logger.Stop(_session));
            }
        }

        public static async Task<IDisposable> StartAsync(string filename)
        {
            var providers = new ProviderInfo[]
            {
                new KernelProviderInfo() {Keywords = (ulong)KernelKeywords, StackKeywords = (ulong)KernelStackKeywords },
                new UserProviderInfo() { ProviderGuid = BenchmarkEventSourceGuid },
                new UserProviderInfo() { ProviderGuid = ClrTraceEventParser.ProviderGuid, Keywords = (ulong)ClrKeywords, Level = TraceEventLevel.Informational},
                new UserProviderInfo() {ProviderGuid =  ClrRundownTraceEventParser.ProviderGuid, Keywords = (ulong)ClrRundownKeywords, Level = TraceEventLevel.Informational},
            };

            var sessionName = await _loggerDomain.ExecuteAsync(() => Logger.Start(filename, providers, 128));

            return new Stopper(sessionName);
        }
    }
}
