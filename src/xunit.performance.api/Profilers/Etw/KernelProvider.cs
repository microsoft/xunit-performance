// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing.Parsers;

namespace Microsoft.Xunit.Performance.Api.Profilers.Etw
{
    /// <summary>
    /// Provides a simple interface to ETW kernel providers.
    /// </summary>
    sealed class KernelProvider
    {
        static KernelProvider() => Default = new KernelProvider
        {
            Flags = KernelTraceEventParser.Keywords.ImageLoad
                    | KernelTraceEventParser.Keywords.Process
                    | KernelTraceEventParser.Keywords.Profile
                    | KernelTraceEventParser.Keywords.Thread,
            StackCapture = KernelTraceEventParser.Keywords.None,
        };

        /// <summary>
        /// Default kernel flags enabled by the xUnit-Performance Api.
        /// </summary>
        public static KernelProvider Default { get; }

        /// <summary>
        /// Specifies the particular kernel events of interest.
        /// </summary>
        public KernelTraceEventParser.Keywords Flags { get; set; }

        /// <summary>
        /// Specifies which events should have their stack traces captured when an event is logged.
        /// </summary>
        public KernelTraceEventParser.Keywords StackCapture { get; set; }
    }
}