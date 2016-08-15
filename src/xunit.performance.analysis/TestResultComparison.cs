// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Xunit.Performance.Analysis
{

    internal class TestResultComparison
    {
        public string TestName;
        public TestResult BaselineResult;
        public TestResult ComparisonResult;
        public double PercentChange;
        public double PercentChangeError;
        public MetricInfo MetricInfo;

        public double SortChange => (PercentChange > 0) ? Math.Max(PercentChange - PercentChangeError, 0) : Math.Min(PercentChange + PercentChangeError, 0);

        public bool? Passed
        {
            get
            {
                if (MetricInfo != null && PercentChange > 0 && PercentChange > PercentChangeError)
                    return MetricInfo.BetterDirection.ToLowerInvariant() == "asc" ? true : false;
                if (MetricInfo != null && PercentChange < 0 && PercentChange < -PercentChangeError)
                    return MetricInfo.BetterDirection.ToLowerInvariant() == "desc" ? true : false;
                else
                    return null;
            }
        }
    }
}
