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
    internal sealed class ConsoleDiagnosticMessageSink : TestMessageVisitor<IDiagnosticMessage>
    {
        protected override bool Visit(IDiagnosticMessage diagnosticMessage)
        {
            WriteErrorLine(diagnosticMessage.Message);
            return true;
        }
    }
}
