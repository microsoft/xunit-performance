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

