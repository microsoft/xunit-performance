// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing;
using Microsoft.Xunit.Performance.Sdk;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance
{
    internal class Program : ProgramCore
    {
        private static int Main(string[] args)
        {
            return new Program().Run(args);
        }

        protected override IPerformanceMetricLogger GetPerformanceMetricLogger(XunitPerformanceProject project)
        {
            return new EtwPerformanceMetricLogger(project, this);
        }

        protected override string GetRuntimeVersion()
        {
            return Environment.Version.ToString();
        }
    }
}
