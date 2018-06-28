// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Xunit.Performance.Api.Native.Windows;
using System;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Api.Profilers.Etw
{
    /// <summary>
    /// Provides a basic interface for ETW Tracing operations.
    /// </summary>
    static class Helper
    {
        static Helper()
        {
            AvailablePreciseMachineCounters = TraceEventProfileSources.GetInfo();
            CanEnableKernelProvider = TraceEventSession.IsElevated() == true;
        }

        /// <summary>
        /// Collection of available precise machine counters.
        /// </summary>
        public static IReadOnlyDictionary<string, ProfileSourceInfo> AvailablePreciseMachineCounters { get; }

        /// <summary>
        /// Indicates whether an ETW kernel session can be enabled.
        /// </summary>
        public static bool CanEnableKernelProvider { get; }

        /// <summary>
        /// Creates a ETW kernel session data object.
        /// </summary>
        /// <param name="kernelFileName">Name of the etl file where the kernel session events will be written to.</param>
        /// <param name="bufferSizeMB">Sizeof of the internal buffer to be used by the kernel session.</param>
        /// <returns>A new Session data object.</returns>
        public static Session MakeKernelSession(string kernelFileName, int bufferSizeMB) => new Session(new SessionData(KernelTraceEventParser.KernelSessionName, kernelFileName)
        {
            BufferSizeMB = bufferSizeMB
        });

        /// <summary>
        /// Determines whether a kernel session is needed.
        /// </summary>
        /// <param name="kernelProvider">Provider data that will be traced.</param>
        /// <returns>True if a kernel session is needed, False otherwise.</returns>
        public static bool NeedSeparateKernelSession(KernelProvider kernelProvider)
        {
            if (kernelProvider == null)
                throw new ArgumentNullException(nameof(kernelProvider));

            // CPU counters need the special kernel session
            return ((kernelProvider.Flags & (KernelTraceEventParser.Keywords.Profile | KernelTraceEventParser.Keywords.PMCProfile))
                != KernelTraceEventParser.Keywords.None);
        }

        /// <summary>
        /// Enable the PMC machine wide, for ETW capture.
        /// </summary>
        /// <param name="profileSourceInfos">Collection of PMC to be enabled.</param>
        public static void SetPreciseMachineCounters(IReadOnlyCollection<ProfileSourceInfo> profileSourceInfos)
        {
            if (profileSourceInfos == null)
                throw new ArgumentNullException(nameof(profileSourceInfos));
            if (!Kernel32.IsWindows8OrGreater())
                throw new InvalidOperationException("System Tracing is only supported on Windows 8 and above.");

            var profileSourceIDs = new List<int>();
            var profileSourceIntervals = new List<int>();

            foreach (var psi in profileSourceInfos)
            {
                if (AvailablePreciseMachineCounters.TryGetValue(psi.Name, out var profInfo))
                {
                    profileSourceIDs.Add(profInfo.ID);
                    profileSourceIntervals.Add(Math.Min(profInfo.MaxInterval, Math.Max(profInfo.MinInterval, psi.Interval)));
                }
            }

            if (profileSourceIDs.Count > 0)
            {
                //
                // FIXME: This function changes the -pmcsources intervals machine wide.
                //  Maybe we should undo/revert these changes!
                //
                TraceEventProfileSources.Set(profileSourceIDs.ToArray(), profileSourceIntervals.ToArray());
            }
        }
    }
}