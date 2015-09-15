// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xunit.Performance.Sdk;

namespace Microsoft.Xunit.Performance
{
    internal class ProgramPortable : ProgramCore
    {
        public static int Main(string[] args)
        {
            return new ProgramPortable().Run(args);
        }

        private static string GetCSVPath(string pathBase) => pathBase + ".csv";

        internal override IPerformanceMetricReader GetPerformanceMetricReader(IEnumerable<PerformanceTestInfo> tests, string pathBase, string runId)
        {
            return new PortableMetricReader(GetCSVPath(pathBase));
        }

        internal override string GetRuntimeVersion()
        {
            return "Portable";
        }

        internal override IDisposable StartTracing(IEnumerable<PerformanceTestInfo> tests, string pathBase)
        {
            Environment.SetEnvironmentVariable("XUNIT_PERFORMANCE_FILE_LOG_PATH", GetCSVPath(pathBase));
            return null;
        }
    }
}
