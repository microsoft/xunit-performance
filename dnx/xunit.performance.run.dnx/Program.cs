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
    internal class Program : ProgramCore
    {
        public static int Main(string[] args)
        {
            return new Program().Run(args);
        }

        protected override IPerformanceMetricLogger GetPerformanceMetricLogger(XunitPerformanceProject project)
        {
            return new CSVMetricLogger(project);
        }

        protected override string GetRuntimeVersion()
        {
            return "Portable";
        }
    }
}
