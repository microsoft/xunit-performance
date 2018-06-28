// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Xunit.Performance
{
    public sealed class BenchmarkConfiguration
    {
        static BenchmarkConfiguration s_instance;

        BenchmarkConfiguration()
        {
        }

        public static BenchmarkConfiguration Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = new BenchmarkConfiguration();
                }

                return s_instance;
            }
        }

        public string FileLogPath { get; set; } = Environment.GetEnvironmentVariable("XUNIT_PERFORMANCE_FILE_LOG_PATH");
        public int MaxIteration { get; set; } = int.Parse(Environment.GetEnvironmentVariable("XUNIT_PERFORMANCE_MAX_ITERATION") ?? "1000");
        public int MaxIterationWhenInnerSpecified { get; set; } = int.Parse(Environment.GetEnvironmentVariable("XUNIT_PERFORMANCE_MAX_ITERATION_INNER_SPECIFIED") ?? "100");
        public int MaxTotalMilliseconds { get; set; } = int.Parse(Environment.GetEnvironmentVariable("XUNIT_PERFORMANCE_MAX_TOTAL_MILLISECONDS") ?? "10000");
        public int MinIteration { get; set; } = int.Parse(Environment.GetEnvironmentVariable("XUNIT_PERFORMANCE_MIN_ITERATION") ?? "1");
        public string RunId { get; set; } = Environment.GetEnvironmentVariable("XUNIT_PERFORMANCE_RUN_ID");
        public bool RunningAsPerfTest { get; set; } = true;
    }
}