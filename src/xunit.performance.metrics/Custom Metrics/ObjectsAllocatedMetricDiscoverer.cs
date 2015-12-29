using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance
{
    internal class ObjectsAllocatedMetricDiscoverer : IPerformanceMetricDiscoverer
    {
        public IEnumerable<PerformanceMetricInfo> GetMetrics(IAttributeInfo metricAttribute)
        {
            yield return new ObjectsAllocatedMetric();
        }

        private class ObjectsAllocatedMetric : PerformanceMetric
        {
            public ObjectsAllocatedMetric()
                : base("ObjectsAllocated", "Objects Allocated", PerformanceMetricUnits.List)
            {
            }

            public override IEnumerable<ProviderInfo> ProviderInfo
            {
                get
                {
                    yield return new UserProviderInfo()
                    {
                        ProviderGuid = ETWClrProfilerTraceEventParser.ProviderGuid,
                        Level = TraceEventLevel.Verbose,
                        Keywords = (ulong)(ETWClrProfilerTraceEventParser.Keywords.Call
                                         | ETWClrProfilerTraceEventParser.Keywords.CallSampled
                                         | ETWClrProfilerTraceEventParser.Keywords.GCAlloc
                                         | ETWClrProfilerTraceEventParser.Keywords.GCAllocSampled)
                    };
                }
            }

            public override PerformanceMetricEvaluator CreateEvaluator(PerformanceMetricEvaluationContext context)
            {
                return new ObjectsAllocatedEvaluator(context);
            }
        }

        private class ObjectsAllocatedEvaluator : PerformanceMetricEvaluator
        {
            private readonly PerformanceMetricEvaluationContext _context;
            private ListMetricInfo _objects;
            private static Dictionary<string, string> _objectNameDict = new Dictionary<string, string>();

            public ObjectsAllocatedEvaluator(PerformanceMetricEvaluationContext context)
            {
                _context = context;
                context.TraceEventSource.Dynamic.AddCallbackForProviderEvent("ETWClrProfiler","ClassIDDefinition", Parser_ClassIDDefinition);
                context.TraceEventSource.Dynamic.AddCallbackForProviderEvent("ETWClrProfiler","ObjectAllocated", Parser_ObjectAllocated);
            }

            private void Parser_ClassIDDefinition(TraceEvent data)
            {
                if (_context.IsTestEvent(data))
                {
                    var classID = data.PayloadByName("ClassID").ToString();
                    var className = data.PayloadByName("Name").ToString();
                    if (_objectNameDict.ContainsKey(classID))
                        throw new System.Exception($"Duplicate class ID found. Class ID: {classID} Class Name: {className}");
                    _objectNameDict[classID] = className;
                }
            }

            private void Parser_ObjectAllocated(TraceEvent data)
            {
                if (_context.IsTestEvent(data))
                {
                    //_objects.addItem(data.FileName, data.IoSize);
                    var classID = data.PayloadByName("ClassID").ToString();
                    var className = _objectNameDict[classID];
                    var size = (long)data.PayloadByName("Size");
                    _objects.addItem(className, size);
                }
            }

            public override void BeginIteration(TraceEvent beginEvent)
            {
                _objects = new ListMetricInfo();
            }

            public override object EndIteration(TraceEvent endEvent)
            {
                return _objects;
            }
        }
    }
}
