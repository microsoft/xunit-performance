using System;
using System.Collections.Generic;
using Microsoft.ProcessDomain;
using System.Collections.Concurrent;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing;

namespace Microsoft.Xunit.Performance
{
    public static class Logger
    {
        private static bool _unloadHandlerRegistered;
        private static ConcurrentDictionary<string, TraceEventSession> _sessions = new ConcurrentDictionary<string, TraceEventSession>();

        private static void EnsureUnloadHandlerRegistered()
        {
            if (!_unloadHandlerRegistered)
            {
                ProcDomain.GetCurrentProcDomain().Unloading += Logger_Unloading;
                _unloadHandlerRegistered = true;
            }
        }

        private static void Logger_Unloading(ProcDomain obj)
        {
            foreach (var session in _sessions)
            {
                session.Value.Dispose();
            }
        }

        [ProcDomainExport]
        public static string Start(string etlPath, IEnumerable<ProviderInfo> providerInfo, int bufferSizeMB = 64)
        {
            var sessionName = "xunit.performance.logger." + Guid.NewGuid().ToString();
            var session = new TraceEventSession(sessionName, etlPath);

            try
            {
                session.BufferSizeMB = bufferSizeMB;

                foreach (var info in ProviderInfo.Merge(providerInfo))
                {
                    var kernelInfo = info as KernelProviderInfo;
                    if (kernelInfo != null)
                        session.EnableKernelProvider((KernelTraceEventParser.Keywords)kernelInfo.Keywords, (KernelTraceEventParser.Keywords)kernelInfo.StackKeywords);

                    var userInfo = info as UserProviderInfo;
                    if (userInfo != null)
                        session.EnableProvider(userInfo.ProviderGuid, (TraceEventLevel)userInfo.Level, userInfo.Keywords);
                }
            }
            catch
            {
                session.Dispose();
                throw;
            }

            return sessionName;
        }

        [ProcDomainExport]
        public static void Stop(string sessionName)
        {
            TraceEventSession session;
            if (_sessions.TryRemove(sessionName, out session))
            {
                session.Stop(noThrow: true);
                session.Dispose();
            }
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("This program is automatically executed by the xunit.performance infrastructure.");
                Environment.Exit(1);
            }

            ProcDomain.HostDomain(args[0]);
        }
    }
}
