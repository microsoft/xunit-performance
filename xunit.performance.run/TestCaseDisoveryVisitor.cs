using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance
{
    internal class TestCaseDisoveryVisitor : TestMessageVisitor<IDiscoveryCompleteMessage>
    {
        public readonly List<ITestCase> Tests = new List<ITestCase>();

        protected override bool Visit(ITestCaseDiscoveryMessage testCaseDiscovered)
        {
            Tests.Add(testCaseDiscovered.TestCase);
            return true;
        }
    }
}
