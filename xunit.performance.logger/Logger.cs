using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ProcessDomain;
using System.Collections.Concurrent;
using Microsoft.Diagnostics.Tracing.Session;

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
                session.Value.Stop(noThrow: true);
        }

        [ProcDomainExport]
        public static string Start(string etlPath, IEnumerable<ProviderInfo> providerInfo, int bufferSizeMB = 64)
        {
            var sessionName = "xunit.performance.logger." + Guid.NewGuid().ToString();
            var session = new TraceEventSession(sessionName, etlPath);

            try
            {
                session.BufferSizeMB = bufferSizeMB;

                foreach (var info in providerInfo)
                    info.Enable(session);
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
