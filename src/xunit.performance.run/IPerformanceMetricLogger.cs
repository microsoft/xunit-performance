// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Xunit.Performance.Sdk
{
    public interface IPerformanceMetricLogger
    {
        IDisposable StartLogging();
        IPerformanceMetricReader GetReader();
    }
}