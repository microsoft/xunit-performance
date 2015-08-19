using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Xunit.Performance.Sdk
{
    public sealed class PerformanceMetricEvaluationContext
    {
        HashSet<int> _currentProcesses = new HashSet<int>();

        internal PerformanceMetricEvaluationContext(TraceEventSource traceEventSource)
        {
            TraceEventSource = traceEventSource;
        }

        public TraceEventSource TraceEventSource { get; private set; }

        public bool IsTestEvent(TraceEvent traceEvent) => _currentProcesses.Contains(traceEvent.ProcessID);
    }
}
