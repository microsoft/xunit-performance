// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Analysis
{
    internal class TestResult
    {
        public string TestName;
        public string RunId;
        public Dictionary<string, TestStatistics> Stats = new Dictionary<string, TestStatistics>();
        public List<TestIterationResult> Iterations = new List<TestIterationResult>();
    }
}
