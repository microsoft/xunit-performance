// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Xunit.Performance
{
    class BenchmarkTestFrameworkTypeDiscoverer : ITestFrameworkTypeDiscoverer
    {
        public Type GetTestFrameworkType(IAttributeInfo attribute)
        {
            if (BenchmarkConfiguration.Instance.RunningAsPerfTest)
                return typeof(BenchmarkTestFramework);
            else
                return typeof(XunitTestFramework);
        }
    }
}