// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.ProcessDomain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Xunit.Performance
{
    internal static class ETWLogging
    {
        private static readonly Guid s_benchmarkEventSourceGuid = Guid.Parse("A3B447A8-6549-4158-9BAD-76D442A47061");

        private static readonly ProviderInfo[] s_requiredProviders = new ProviderInfo[]
        {
            new KernelProviderInfo()
            {
                Keywords = (ulong)KernelTraceEventParser.Keywords.Process | (ulong)KernelTraceEventParser.Keywords.Profile,
                StackKeywords = (ulong)KernelTraceEventParser.Keywords.Profile
            },
            new UserProviderInfo()
            {
                ProviderGuid = s_benchmarkEventSourceGuid,
                Level = TraceEventLevel.Verbose,
                Keywords = ulong.MaxValue,
            },
            new UserProviderInfo()
            {
                ProviderGuid = ClrTraceEventParser.ProviderGuid,
                Level = TraceEventLevel.Informational,
                Keywords =
                (
                    (ulong)ClrTraceEventParser.Keywords.Jit |
                    (ulong)ClrTraceEventParser.Keywords.JittedMethodILToNativeMap |
                    (ulong)ClrTraceEventParser.Keywords.Loader |
                    (ulong)ClrTraceEventParser.Keywords.Exception |
                    (ulong)ClrTraceEventParser.Keywords.GC
                ),
            }
        };

        private static readonly ProcDomain s_loggerDomain = ProcDomain.CreateDomain(nameof(Logger), typeof(Logger), runElevated: true);

        private class Stopper : IDisposable
        {
            private string _session;
            public Stopper(string session) { _session = session; }
            public void Dispose()
            {
                s_loggerDomain.ExecuteAsync(() => Logger.Stop(_session)).Wait();
            }
        }

        public static async Task<IDisposable> StartAsync(string etlPath, IEnumerable<ProviderInfo> providers)
        {
            var allProviders = s_requiredProviders.Concat(providers).ToArray();
            var sessionName = await s_loggerDomain.ExecuteAsync(() => Logger.Start(etlPath, allProviders, 128));
            return new Stopper(sessionName);
        }
    }
}
