using System;
using Xunit.Abstractions;

namespace Xunit.TestDiscoverer
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
