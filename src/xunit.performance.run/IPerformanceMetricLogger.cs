// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Xunit.Performance.Sdk
{
    public interface IPerformanceMetricLogger
    {
        IDisposable StartLogging(ProcessStartInfo runnerStartInfo);
        IPerformanceMetricReader GetReader();
    }
}