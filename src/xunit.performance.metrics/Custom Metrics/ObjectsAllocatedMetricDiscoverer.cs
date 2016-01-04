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
            private static ListMetricInfo _objects = new ListMetricInfo();
            private static Dictionary<string, string> _objectNameDict = new Dictionary<string, string>();

            public ObjectsAllocatedEvaluator(PerformanceMetricEvaluationContext context)
            {
                _context = context;
                var etwClrProfilerTraceEventParser = new ETWClrProfilerTraceEventParser(context.TraceEventSource);
                etwClrProfilerTraceEventParser.ClassIDDefintion += Parser_ClassIDDefinition;
                etwClrProfilerTraceEventParser.ObjectAllocated += Parser_ObjectAllocated;
            }

            private void Parser_ClassIDDefinition(Microsoft.Diagnostics.Tracing.Parsers.ETWClrProfiler.ClassIDDefintionArgs data)
            {
                var classID = data.ClassID.ToString();
                var className = data.Name;
                //if (_objectNameDict.ContainsKey(classID))
                //    throw new System.Exception($"Duplicate class ID found. Class ID: {classID} Class Name: {className}");
                _objectNameDict[classID] = className;
            }

            private void Parser_ObjectAllocated(Microsoft.Diagnostics.Tracing.Parsers.ETWClrProfiler.ObjectAllocatedArgs data)
            {
                if (_context.IsTestEvent(data))
                {
                    //_objects.addItem(data.FileName, data.IoSize);
                    var classID = data.ClassID.ToString();
                    var className = _objectNameDict[classID];
                    var size = data.Size;
                    _objects.addItem(className, size);
                }
            }

            public override void BeginIteration(TraceEvent beginEvent)
            {
                _objects.clear();
            }

            public override object EndIteration(TraceEvent endEvent)
            {
                return _objects;
            }
        }
    }
}
