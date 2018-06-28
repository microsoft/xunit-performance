// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Xunit.Performance
{
    class BenchmarkTestRunner : XunitTestRunner
    {
        public BenchmarkTestRunner(ITest test,
                               IMessageBus messageBus,
                               Type testClass,
                               object[] constructorArguments,
                               MethodInfo testMethod,
                               object[] testMethodArguments,
                               string skipReason,
                               IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes,
                               ExceptionAggregator aggregator,
                               CancellationTokenSource cancellationTokenSource)
            : base(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, skipReason, beforeAfterAttributes, aggregator, cancellationTokenSource)
        {
        }

        protected override Task<decimal> InvokeTestMethodAsync(ExceptionAggregator aggregator) => new BenchmarkTestInvoker(Test, MessageBus, TestClass, ConstructorArguments, TestMethod, TestMethodArguments, BeforeAfterAttributes, aggregator, CancellationTokenSource).RunAsync();
    }
}