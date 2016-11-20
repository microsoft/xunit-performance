// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Xunit.Performance
{
    public sealed class BenchmarkConfiguration
    {
        private static BenchmarkConfiguration s_instance;

        private string _runId = Environment.GetEnvironmentVariable("XUNIT_PERFORMANCE_RUN_ID");
        private int _minIteration = int.Parse(Environment.GetEnvironmentVariable("XUNIT_PERFORMANCE_MIN_ITERATION") ?? "1");
        private int _maxIteration = int.Parse(Environment.GetEnvironmentVariable("XUNIT_PERFORMANCE_MAX_ITERATION") ?? "1000");
        private int _maxIterationWhenInnerSpecified = int.Parse(Environment.GetEnvironmentVariable("XUNIT_PERFORMANCE_MAX_ITERATION_INNER_SPECIFIED") ?? "100");
        private int _maxTotalMilliseconds = int.Parse(Environment.GetEnvironmentVariable("XUNIT_PERFORMANCE_MAX_TOTAL_MILLISECONDS") ?? "10000");
        private string _fileLogPath = Environment.GetEnvironmentVariable("XUNIT_PERFORMANCE_FILE_LOG_PATH");
        private bool _runningAsPerfTest = true;

        private BenchmarkConfiguration()
        {
        }

        public static BenchmarkConfiguration Instance
        {
            get
            {
                if(s_instance == null)
                {
                    s_instance = new BenchmarkConfiguration();
                }

                return s_instance;
            }
        }

        public string RunId
        {
            get { return _runId; }
            set { _runId = value; }
        }

        public  int MinIteration
        {
            get { return _minIteration; }
            set { _minIteration = value; }
        }

        public int MaxIteration
        {
            get { return _maxIteration; }
            set { _maxIteration = value; }
        }

        public int MaxIterationWhenInnerSpecified
        {
            get { return _maxIterationWhenInnerSpecified; }
            set { _maxIterationWhenInnerSpecified = value; }
        }

        public int MaxTotalMilliseconds
        {
            get { return _maxTotalMilliseconds; }
            set { _maxTotalMilliseconds = value; }
        }

        public string FileLogPath
        {
            get { return _fileLogPath; }
            set { _fileLogPath = value; }
        }

        public bool RunningAsPerfTest
        {
            get { return _runningAsPerfTest; }
            set { _runningAsPerfTest = value; }
        }
    }
}
