using System;
using Xunit.Abstractions;

namespace Xunit.TestDiscoverer
{
    internal class TestCaseDisoveryVisitor : TestMessageVisitor<IDiscoveryCompleteMessage>
    {
        private readonly Action<ITestCase> _testDiscoveredAction;

        public TestCaseDisoveryVisitor(Action<ITestCase> testDiscoveredAction)
        {
            _testDiscoveredAction = testDiscoveredAction;
        }

        protected override bool Visit(ITestCaseDiscoveryMessage testCaseDiscovered)
        {
            _testDiscoveredAction(testCaseDiscovered.TestCase);
            return true;
        }
    }
}