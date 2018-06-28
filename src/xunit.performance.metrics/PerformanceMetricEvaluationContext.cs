// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing;

namespace Microsoft.Xunit.Performance.Sdk
{
    public abstract class PerformanceMetricEvaluationContext
    {
        public abstract TraceEventSource TraceEventSource { get; }

        public abstract bool IsTestEvent(TraceEvent traceEvent);
    }
}