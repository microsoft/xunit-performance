// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Xunit.Abstractions;
using static Microsoft.Xunit.Performance.Api.PerformanceLogger;

namespace Microsoft.Xunit.Performance.Api
{
    /// <summary>
    /// This is the message sink that receives IDiagnosticMessage messages from
    /// the XunitFrontController.
    /// </summary>
    internal sealed class ConsoleDiagnosticMessageSink : TestMessageSink
    {
        public ConsoleDiagnosticMessageSink()
        {
            Diagnostics.DiagnosticMessageEvent += OnDiagnosticMessageEvent;
        }

        private static void OnDiagnosticMessageEvent(MessageHandlerArgs<IDiagnosticMessage> args)
        {
            WriteErrorLine(args.Message.Message);
        }

        public override void Dispose()
        {
            Diagnostics.DiagnosticMessageEvent -= OnDiagnosticMessageEvent;

            base.Dispose();
        }
    }
}
