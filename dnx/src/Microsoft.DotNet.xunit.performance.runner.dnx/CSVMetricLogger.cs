// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance.Sdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Xunit.Performance
{
    internal class CSVMetricLogger : IPerformanceMetricLogger
    {
        private readonly string _csvPath;

        public CSVMetricLogger(XunitPerformanceProject project)
        {
            _csvPath = Path.GetFullPath(Path.Combine(project.OutputDir, project.RunId + ".csv"));
        }

        public IDisposable StartLogging(System.Diagnostics.ProcessStartInfo runnerStartInfo)
        {
            runnerStartInfo.Environment["XUNIT_PERFORMANCE_FILE_LOG_PATH"] = _csvPath;
            return null;
        }

        public IPerformanceMetricReader GetReader()
        {
            return new CSVMetricReader(_csvPath);
        }
    }
}
