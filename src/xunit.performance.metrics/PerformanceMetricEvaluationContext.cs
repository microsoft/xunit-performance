using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Xunit.Performance.Sdk
{
    public abstract class PerformanceMetricEvaluationContext
    {
        public abstract TraceEventSource TraceEventSource { get; }

        public abstract bool IsTestEvent(TraceEvent traceEvent);
    }
}
