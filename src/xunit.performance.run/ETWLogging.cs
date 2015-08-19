using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.ProcessDomain;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Xunit.Performance
{
    static class ETWLogging
    {
        static readonly Guid BenchmarkEventSourceGuid = Guid.Parse("A3B447A8-6549-4158-9BAD-76D442A47061");

        static readonly ProviderInfo[] RequiredProviders = new ProviderInfo[]
        {
            new KernelProviderInfo()
            {
                Keywords = (ulong)(KernelTraceEventParser.Keywords.Process | KernelTraceEventParser.Keywords.Profile),
                StackKeywords = (ulong)(KernelTraceEventParser.Keywords.Profile)
            },
            new UserProviderInfo()
            {
                ProviderGuid = BenchmarkEventSourceGuid,
                Level = TraceEventLevel.Verbose,
                Keywords = 0xffffffffffffffff,
            },
            new UserProviderInfo()
            {
                ProviderGuid = ClrTraceEventParser.ProviderGuid,
                Level = TraceEventLevel.Informational,
                Keywords = (ulong)
                (
                    ClrTraceEventParser.Keywords.Jit |
                    ClrTraceEventParser.Keywords.JittedMethodILToNativeMap |
                    ClrTraceEventParser.Keywords.Loader |
                    ClrTraceEventParser.Keywords.Exception |
                    ClrTraceEventParser.Keywords.GC
                ),
            }
        };

        public static ProcDomain _loggerDomain = ProcDomain.CreateDomain("Logger", ".\\xunit.performance.logger.exe", runElevated: true);

        private class Stopper : IDisposable
        {
            private string _session;
            public Stopper(string session) { _session = session; }
            public void Dispose()
            {
                _loggerDomain.ExecuteAsync(() => Logger.Stop(_session));
            }
        }

        public static async Task<IDisposable> StartAsync(string filename, IEnumerable<ProviderInfo> providers)
        {
            var allProviders = RequiredProviders.Concat(providers).ToArray();
            var sessionName = await _loggerDomain.ExecuteAsync(() => Logger.Start(filename, allProviders, 128));
            return new Stopper(sessionName);
        }
    }
}
