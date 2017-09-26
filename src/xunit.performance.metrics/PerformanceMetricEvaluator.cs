// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing;
using System;

namespace Microsoft.Xunit.Performance.Sdk
{
    public abstract class PerformanceMetricEvaluator : IDisposable
    {
        public abstract void BeginIteration(TraceEvent beginEvent);

        public abstract double EndIteration(TraceEvent endEvent);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~PerformanceMetricEvaluator()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
