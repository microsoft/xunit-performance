// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Analysis
{
    internal class TestIterationResult
    {
        public string EtlPath;
        public string RunId;
        public string TestName;
        public int TestIteration;
        public Dictionary<string, double> MetricValues = new Dictionary<string, double>();
    }
}
