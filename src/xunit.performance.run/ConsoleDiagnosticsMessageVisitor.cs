// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance
{
    internal class ConsoleDiagnosticsMessageVisitor : TestMessageVisitor<IDiagnosticMessage>
    {
        protected override bool Visit(IDiagnosticMessage diagnosticMessage)
        {
            Console.Error.WriteLine(diagnosticMessage.Message);
            return true;
        }
    }
}

