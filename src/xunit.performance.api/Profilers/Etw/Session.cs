// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Diagnostics;

namespace Microsoft.Xunit.Performance.Api.Profilers.Etw
{
    /// <summary>
    /// Provides an interface a to TraceEventSession object that is hooked to a
    /// 'Terminate' event handler.
    /// This adds an extra level of protection when the application recording
    /// ETW events through xUnit-Performance API is manually kill by the user.
    /// The terminate handler will attempt to stop the recording ETW session
    /// before exiting, so resources are not held even when the application has
    /// terminated.
    /// </summary>
    sealed class Session : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of a Session class with the provider data.
        /// </summary>
        /// <param name="sessionData"></param>
        public Session(SessionData sessionData)
        {
            _disposedValue = false;
            TraceEventSessionHandle = new SafeTerminateHandler<TraceEventSession>(() =>
                new TraceEventSession(sessionData.Name, sessionData.FileName)
                {
                    BufferSizeMB = sessionData.BufferSizeMB,
                });
        }

        TraceEventSession TraceEventSession => TraceEventSessionHandle.BaseDisposableObject;

        SafeTerminateHandler<TraceEventSession> TraceEventSessionHandle { get; }

        /// <summary>
        /// Interface to enable kernel providers.
        /// </summary>
        /// <param name="flags">Specifies the particular kernel events of interest.</param>
        /// <param name="stackCapture">Specifies which events should have their stack traces captured when an event is logged.</param>
        public void EnableKernelProvider(KernelTraceEventParser.Keywords flags, KernelTraceEventParser.Keywords stackCapture)
        {
            if (TraceEventSession.EnableKernelProvider(flags, stackCapture))
                Debug.WriteLine("The session existed before and needed to be restarted.");
        }

        /// <summary>
        /// Interface to enable user providers.
        /// </summary>
        /// <param name="providerGuid">The Guid that represents the event provider to be enabled.</param>
        /// <param name="providerLevel">The verbosity to turn on.</param>
        /// <param name="matchAnyKeywords">A bitvector representing the areas to turn on. Only the
        /// low 32 bits are used by classic providers and passed as the 'flags' value. Zero
        /// is a special value which is a provider defined default, which is usually 'everything'.
        /// </param>
        /// <param name="options">Additional options for the provider.</param>
        public void EnableProvider(
            Guid providerGuid,
            TraceEventLevel providerLevel = TraceEventLevel.Verbose,
            ulong matchAnyKeywords = ulong.MaxValue,
            TraceEventProviderOptions options = null)
        {
            if (TraceEventSession.EnableProvider(providerGuid, providerLevel, matchAnyKeywords, options))
                Debug.WriteLine("The session already existed and needed to be restarted.");
        }

        #region IDisposable Support

        bool _disposedValue; // To detect redundant calls

        ~Session()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                TraceEventSessionHandle.Dispose();
                _disposedValue = true;
            }
        }

        #endregion IDisposable Support
    }
}