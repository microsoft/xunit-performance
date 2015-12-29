using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance
{
    internal class DllsLoadedMetricDiscoverer : IPerformanceMetricDiscoverer
    {
        public IEnumerable<PerformanceMetricInfo> GetMetrics(IAttributeInfo metricAttribute)
        {
            yield return new DllsLoadedMetric();
        }

        private class DllsLoadedMetric : PerformanceMetric
        {
            public DllsLoadedMetric()
                : base("DllsLoaded", "Dlls Loaded", PerformanceMetricUnits.List)
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
                return new DllsLoadedEvaluator(context);
            }
        }

        private class DllsLoadedEvaluator : PerformanceMetricEvaluator
        {
            private readonly PerformanceMetricEvaluationContext _context;
            private ListMetricInfo _objects;
            private static Dictionary<string, string> _objectModuleDict = new Dictionary<string, string>();
            private static Dictionary<string, string> _objectNameDict = new Dictionary<string, string>();
            private static Dictionary<string, string> _moduleNameDict = new Dictionary<string, string>();

            public DllsLoadedEvaluator(PerformanceMetricEvaluationContext context)
            {
                _context = context;
                context.TraceEventSource.Dynamic.AddCallbackForProviderEvent("ETWClrProfiler", "ClassIDDefinition", Parser_ClassIDDefinition);
                context.TraceEventSource.Dynamic.AddCallbackForProviderEvent("ETWClrProfiler","ModuleIDDefinition", Parser_ModuleIDDefinition);
                context.TraceEventSource.Dynamic.AddCallbackForProviderEvent("ETWClrProfiler","ObjectAllocated", Parser_ObjectAllocated);
            }

            private void Parser_ModuleIDDefinition(TraceEvent data)
            {
                if (_context.IsTestEvent(data))
                {
                    var moduleID = data.PayloadByName("ModuleID").ToString();
                    var moduleName = data.PayloadByName("Path").ToString();

                    if (_moduleNameDict.ContainsKey(moduleID))
                        throw new System.Exception($"Duplicate module ID found. Module ID: {moduleID} Module Name: {moduleName}");
                    _moduleNameDict[moduleID] = moduleName;
                }
            }

            private void Parser_ClassIDDefinition(TraceEvent data)
            {
                if (_context.IsTestEvent(data))
                {
                    var classID = data.PayloadByName("ClassID").ToString();
                    var className = data.PayloadByName("Name").ToString();
                    var moduleID = data.PayloadByName("ModuleID").ToString();

                    if (moduleID != "0x0")
                    {
                        if (_objectNameDict.ContainsKey(classID) || _objectModuleDict.ContainsKey(classID))
                            throw new System.Exception($"Duplicate class ID found. Class ID: {classID} Class Name: {className}");
                        _objectNameDict[classID] = className;
                        _objectModuleDict[classID] = moduleID;
                    }

                    else
                    {
                        if (!className.EndsWith("[]"))
                            throw new System.Exception($"Cannot find module for class {className}.");
                        var fixedClassName = className.Substring(0, className.Length - 2);
                        if(!_objectModuleDict.TryGetValue(fixedClassName, out moduleID))
                            throw new System.Exception($"Cannot find module for class {className}.");
                        _objectModuleDict[classID] = moduleID;
                    }
                }
            }

            private void Parser_ObjectAllocated(TraceEvent data)
            {
                if (_context.IsTestEvent(data))
                {
                    //_objects.addItem(data.FileName, data.IoSize);
                    var classID = data.PayloadByName("ClassID").ToString();
                    var moduleID = _objectModuleDict[classID];
                    var moduleName = _moduleNameDict[moduleID];
                    var size = (long)data.PayloadByName("Size");
                    _objects.addItem(moduleName, size);
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
