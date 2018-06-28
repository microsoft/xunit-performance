// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Api.Profilers.Etw
{
    /// <summary>
    /// Provides a simple interface for ETW user providers.
    /// </summary>
    sealed class UserProvider
    {
        static UserProvider() => Defaults = new[] {
                new UserProvider {
                    Guid = MicrosoftXunitBenchmarkTraceEventParser.ProviderGuid,
                    Keywords = ulong.MaxValue,
                    Level = TraceEventLevel.Verbose,
                },
                new UserProvider {
                    Guid = ClrTraceEventParser.ProviderGuid,
                    Keywords = (ulong)(ClrTraceEventParser.Keywords.Exception
                        | ClrTraceEventParser.Keywords.GC
                        | ClrTraceEventParser.Keywords.Jit
                        | ClrTraceEventParser.Keywords.Loader
                        | ClrTraceEventParser.Keywords.NGen),
                    Level = TraceEventLevel.Verbose,
                },
            };

        /// <summary>
        /// Default ETW user providers enabled by the xUnit-Performance Api.
        /// </summary>
        public static IReadOnlyCollection<UserProvider> Defaults { get; }

        /// <summary>
        /// The Guid that represents the event provider enable.
        /// </summary>
        public Guid Guid { get; set; } = Guid.Empty;

        /// <summary>
        /// A bitvector representing the areas to turn on.
        /// Only the low 32 bits are used by classic providers and passed as the 'flags' value.
        /// Zero is a special value which is a provider defined default, which is usually 'everything'
        /// </summary>
        public ulong Keywords { get; set; } = ulong.MaxValue;

        /// <summary>
        /// Verbosity to turn on.
        /// </summary>
        public TraceEventLevel Level { get; set; } = TraceEventLevel.Verbose;

        /// <summary>
        /// Additional options for the provider
        /// </summary>
        public TraceEventProviderOptions Options { get; set; } = null;
    }
}