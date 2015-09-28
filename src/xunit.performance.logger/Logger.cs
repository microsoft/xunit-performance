// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.ProcessDomain;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Xunit.Performance
{
    public static class Logger
    {
        private class Sessions
        {
            public string UserFileName;
            public string KernelFileName;
            public string MergedFileName;
            public TraceEventSession UserSession;
            public TraceEventSession KernelSession;

            public void Close()
            {
                if (UserSession != null)
                    UserSession.Dispose();
                if (KernelSession != null)
                    KernelSession.Dispose();
            }
        }

        private static bool s_unloadHandlerRegistered;
        private static ConcurrentDictionary<string, Sessions> s_sessions = new ConcurrentDictionary<string, Sessions>();

        private static bool NeedSeparateKernelSession(ulong kernelKeywords)
        {
            // Prior to Windows 8 (NT 6.2), all kernel events needed the special kernel session.
            var os = Environment.OSVersion;
            if (os.Platform == PlatformID.Win32NT && os.Version.Major <= 6 && os.Version.Minor < 2)
                return true;

            // CPU counters need the special kernel session
            if (((KernelTraceEventParser.Keywords)kernelKeywords & KernelTraceEventParser.Keywords.PMCProfile) != 0)
                return true;

            return false;
        }

        private static void EnsureUnloadHandlerRegistered()
        {
            if (!s_unloadHandlerRegistered)
            {
                ProcDomain.GetCurrentProcDomain().Unloading += Logger_Unloading;
                s_unloadHandlerRegistered = true;
            }
        }

        private static void Logger_Unloading(ProcDomain obj)
        {
            foreach (var sessions in s_sessions.Values)
                sessions.Close();
        }

        [ProcDomainExport]
        public static string Start(string etlPath, IEnumerable<ProviderInfo> providerInfo, int bufferSizeMB = 64)
        {
            EnsureUnloadHandlerRegistered();

            var userSessionName = "xunit.performance.logger." + Guid.NewGuid().ToString();
            Sessions sessions = new Sessions();
            sessions.UserFileName = Path.ChangeExtension(etlPath, ".user.etl");
            sessions.KernelFileName = Path.ChangeExtension(etlPath, ".kernel.etl");
            sessions.MergedFileName = etlPath;

            var mergedProviderInfo = ProviderInfo.Merge(providerInfo);

            try
            {
                sessions.UserSession = new TraceEventSession(userSessionName, sessions.UserFileName);
                sessions.UserSession.BufferSizeMB = bufferSizeMB;

                var availableCpuCounters = TraceEventProfileSources.GetInfo();
                var cpuCounterIds = new List<int>();
                var cpuCounterIntervals = new List<int>();
                foreach (var cpuInfo in mergedProviderInfo.OfType<CpuCounterInfo>())
                {
                    ProfileSourceInfo profInfo;
                    if (availableCpuCounters.TryGetValue(cpuInfo.CounterName, out profInfo))
                    {
                        cpuCounterIds.Add(profInfo.ID);
                        cpuCounterIntervals.Add(Math.Min(profInfo.MaxInterval, Math.Max(profInfo.MinInterval, cpuInfo.Interval)));
                    }
                }

                if (cpuCounterIds.Count > 0)
                    TraceEventProfileSources.Set(cpuCounterIds.ToArray(), cpuCounterIntervals.ToArray());

                var kernelInfo = mergedProviderInfo.OfType<KernelProviderInfo>().FirstOrDefault();
                if (kernelInfo != null && NeedSeparateKernelSession(kernelInfo.Keywords))
                {
                    sessions.KernelSession = new TraceEventSession(KernelTraceEventParser.KernelSessionName, sessions.KernelFileName);
                    sessions.KernelSession.BufferSizeMB = bufferSizeMB;
                }
                else
                {
                    sessions.KernelFileName = sessions.UserFileName;
                    sessions.KernelSession = sessions.UserSession;
                }

                if (kernelInfo != null)
                {
                    var kernelKeywords = (KernelTraceEventParser.Keywords)kernelInfo.Keywords;
                    var kernelStackKeywords = (KernelTraceEventParser.Keywords)kernelInfo.StackKeywords;
                    sessions.KernelSession.EnableKernelProvider(kernelKeywords, kernelStackKeywords);
                }

                foreach (var userInfo in mergedProviderInfo.OfType<UserProviderInfo>())
                    sessions.UserSession.EnableProvider(userInfo.ProviderGuid, userInfo.Level, userInfo.Keywords);

                s_sessions[userSessionName] = sessions;
            }
            catch
            {
                sessions.Close();
                throw;
            }

            return userSessionName;
        }

        [ProcDomainExport]
        public static void Stop(string sessionName)
        {
            Sessions sessions;
            if (s_sessions.TryRemove(sessionName, out sessions))
            {
                sessions.Close();

                var files = sessions.KernelFileName == sessions.UserFileName ? new[] { sessions.KernelFileName } : new[] { sessions.KernelFileName, sessions.UserFileName };

                TraceEventSession.Merge(files, sessions.MergedFileName, TraceEventMergeOptions.Compress);

                if (File.Exists(sessions.UserFileName))
                    File.Delete(sessions.UserFileName);
                if (File.Exists(sessions.KernelFileName))
                    File.Delete(sessions.KernelFileName);
            }
        }

        private static void Main(string[] args)
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
