using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance.Api
{
    internal sealed class ConsoleDiagnosticMessageSink : TestMessageVisitor<IDiagnosticMessage>
    {
        protected override bool Visit(IDiagnosticMessage diagnosticMessage)
        {
            Console.Error.WriteLine(diagnosticMessage.Message);
            return true;
        }
    }
}
