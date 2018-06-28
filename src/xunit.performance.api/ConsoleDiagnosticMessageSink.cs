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
    sealed class ConsoleDiagnosticMessageSink : TestMessageSink
    {
        public ConsoleDiagnosticMessageSink() => Diagnostics.DiagnosticMessageEvent += Diagnostics_DiagnosticMessageEvent;

        static void Diagnostics_DiagnosticMessageEvent(MessageHandlerArgs<IDiagnosticMessage> args)
        {
            var message = args.Message;
            WriteErrorLine(message.Message);
        }
    }
}