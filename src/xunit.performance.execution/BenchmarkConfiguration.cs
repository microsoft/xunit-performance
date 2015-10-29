// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Xunit.Performance
{
    internal static class BenchmarkConfiguration
    {
        public static readonly string RunId = Environment.GetEnvironmentVariable("XUNIT_PERFORMANCE_RUN_ID");
        public static readonly int MinIteration = int.Parse(Environment.GetEnvironmentVariable("XUNIT_PERFORMANCE_MIN_ITERATION") ?? "1");
        public static readonly int MaxIteration = int.Parse(Environment.GetEnvironmentVariable("XUNIT_PERFORMANCE_MAX_ITERATION") ?? "0");
        public static readonly int MaxTotalMilliseconds = int.Parse(Environment.GetEnvironmentVariable("XUNIT_PERFORMANCE_MAX_TOTAL_MILLISECONDS") ?? "0");
        public static readonly string FileLogPath = Environment.GetEnvironmentVariable("XUNIT_PERFORMANCE_FILE_LOG_PATH");
        public static bool RunningAsPerfTest => RunId != null;
    }
}
