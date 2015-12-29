// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.ProcessDomain;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;

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
                UninstallETWClrProfiler();
                if (UserSession != null)
                    UserSession.Dispose();
                if (KernelSession != null)
                    KernelSession.Dispose();
            }
        }

        private static bool s_unloadHandlerRegistered;
        private static ConcurrentDictionary<string, Sessions> s_sessions = new ConcurrentDictionary<string, Sessions>();
        private static string s_ProcessArch;
        private static string s_dotNetKey = @"Software\Microsoft\.NETFramework";

        /// <summary>
        /// Get the name of the architecture of the current process
        /// </summary>
        public static string ProcessArch
        {
            get
            {
                if (s_ProcessArch == null)
                {
                    s_ProcessArch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
                    // This should not be needed, but when I run PerfView under VS from an extension on an X64 machine
                    // the environment variable is wrong.  
                    if (s_ProcessArch == "AMD64" && System.Runtime.InteropServices.Marshal.SizeOf(typeof(IntPtr)) == 4)
                        s_ProcessArch = "x86";
                }
                return s_ProcessArch;
            }
        }

        private static bool IsWin8OrGreater
        {
            get
            {
                var os = Environment.OSVersion;
                Debug.Assert(os.Platform == PlatformID.Win32NT);
                if (os.Version.Major < 6)
                    return false;
                if (os.Version.Major == 6 && os.Version.Minor < 2)
                    return false;
                return true;
            }
        }

        private static bool NeedSeparateKernelSession(ulong kernelKeywords)
        {

            // Prior to Windows 8 (NT 6.2), all kernel events needed the special kernel session.
            if (!IsWin8OrGreater)
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

                if (IsWin8OrGreater)
                {
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
                }

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

                ulong profilerKeywords = 0;

                foreach (var userInfo in mergedProviderInfo.OfType<UserProviderInfo>())
                {
                    sessions.UserSession.EnableProvider(userInfo.ProviderGuid, userInfo.Level, userInfo.Keywords);
                    if (userInfo.ProviderGuid == ETWClrProfilerTraceEventParser.ProviderGuid)
                        profilerKeywords |= userInfo.Keywords;
                }

                if(profilerKeywords != 0)
                    InstallETWClrProfiler((int)profilerKeywords);

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

        /// <summary>
        /// Writes the contents to the temp folder.
        /// </summary>
        /// <param name="fileName">File name to use</param>
        /// <param name="contents">Contents to write</param>
        private static void CopyToTemp(string fileName, Stream contents)
        {
            string tempPath = Path.GetTempPath();
            string file = Path.Combine(tempPath, fileName);
            using (var fileStream = File.Create(file))
            {
                contents.CopyTo(fileStream);
            }
        }

        private static void writeDllToTemp(bool native = false)
        {
            var arch = native ? Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432") : ProcessArch;
            var resourceName = "Microsoft.Xunit.Performance.ETWClrProfiler." + arch.ToLower() + ".ETWClrProfiler.dll";
            var fileName = "ETWClrProfiler_" + arch + ".dll";
            var profilerDll = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if(profilerDll == null)
                throw new System.Exception($"ERROR do not have a ETWClrProfiler.dll for architecture {arch} {resourceName}");

            CopyToTemp(fileName, profilerDll);
        }

        private static void InstallETWClrProfiler(int profilerKeywords)
        {
            // Ensuring that the .NET CLR Profiler is installed.

            writeDllToTemp(false);
            var profilerDll = Path.Combine(Path.GetTempPath(), "ETWClrProfiler_" + ProcessArch + ".dll");

            // Adding HKLM\Software\Microsoft\.NETFramework\COR* registry keys
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(s_dotNetKey))
            {
                var existingValue = key.GetValue("COR_PROFILER") as string;
                if (existingValue != null && "{6652970f-1756-5d8d-0805-e9aad152aa84}" != existingValue)
                {
                    throw new System.Exception($"ERROR there is an existing CLR Profiler {existingValue}.  Doing nothing.");
                }
                key.SetValue("COR_PROFILER", "{6652970f-1756-5d8d-0805-e9aad152aa84}");
                key.SetValue("COR_PROFILER_PATH", profilerDll);
                key.SetValue("COR_ENABLE_PROFILING", 1);
                key.SetValue("PerfView_Keywords", profilerKeywords);
            }

            // Set it up for X64.  
            var nativeArch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432");
            if (nativeArch != null)
            {
                writeDllToTemp(true);
                var profilerNativeDll = Path.Combine(Path.GetTempPath(), "ETWClrProfiler_" + nativeArch + ".dll");
                // Detected 64 bit system, Adding 64 bit HKLM\Software\Microsoft\.NETFramework\COR* registry keys
                using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (RegistryKey key = hklm.CreateSubKey(s_dotNetKey))
                {
                    var existingValue = key.GetValue("COR_PROFILER") as string;
                    if (existingValue != null && "{6652970f-1756-5d8d-0805-e9aad152aa84}" != existingValue)
                    {
                        throw new System.Exception($"ERROR there is an existing CLR Profiler arch {nativeArch} {existingValue}.");
                    }
                    key.SetValue("COR_PROFILER", "{6652970f-1756-5d8d-0805-e9aad152aa84}");
                    key.SetValue("COR_PROFILER_PATH", profilerNativeDll);
                    key.SetValue("COR_ENABLE_PROFILING", 1);
                    key.SetValue("PerfView_Keywords", profilerKeywords);

                }
            }

            // Installed .NET CLR Profiler.
        }

        private static void UninstallETWClrProfiler()
        {
            // Ensuring .NET Allocation profiler not installed.

            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(s_dotNetKey))
            {
                string existingValue = key.GetValue("COR_PROFILER") as string;
                if (existingValue == null)
                    return;
                if (existingValue != "{6652970f-1756-5d8d-0805-e9aad152aa84}")
                {
                    throw new System.Exception($"ERROR trying to remove EtwClrProfiler, found an existing Profiler {existingValue} doing nothing.");
                }
                key.DeleteValue("COR_PROFILER", false);
                key.DeleteValue("COR_PROFILER_PATH", false);
                key.DeleteValue("COR_ENABLE_PROFILING", false);
                key.DeleteValue("PerfView_Keywords", false);

            }
            if (Environment.Is64BitOperatingSystem)
            {
                using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (RegistryKey key = hklm.CreateSubKey(s_dotNetKey))
                {
                    string existingValue = key.GetValue("COR_PROFILER") as string;
                    if (existingValue == null)
                        return;
                    if (existingValue != "{6652970f-1756-5d8d-0805-e9aad152aa84}")
                    {
                        throw new System.Exception($"ERROR trying to remove EtwClrProfiler of X64, found an existing Profiler {existingValue} doing nothing.");
                    }
                    key.DeleteValue("COR_PROFILER", false);
                    key.DeleteValue("COR_PROFILER_PATH", false);
                    key.DeleteValue("COR_ENABLE_PROFILING", false);
                    key.DeleteValue("PerfView_Keywords", false);
                }
            }

            // Uninstalled .NET Allocation profiler.
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
