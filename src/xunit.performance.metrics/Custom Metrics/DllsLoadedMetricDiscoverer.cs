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
            private static ListMetricInfo _objects = new ListMetricInfo();
            private static Dictionary<string, string> _objectModuleDict = new Dictionary<string, string>();
            private static Dictionary<string, string> _className_classIDDict = new Dictionary<string, string>();
            private static Dictionary<string, string> _moduleNameDict = new Dictionary<string, string>();

            public DllsLoadedEvaluator(PerformanceMetricEvaluationContext context)
            {
                _context = context;
                var etwClrProfilerTraceEventParser = new ETWClrProfilerTraceEventParser(context.TraceEventSource);
                etwClrProfilerTraceEventParser.ClassIDDefintion += Parser_ClassIDDefinition;
                etwClrProfilerTraceEventParser.ModuleIDDefintion += Parser_ModuleIDDefinition;
                etwClrProfilerTraceEventParser.ObjectAllocated += Parser_ObjectAllocated;
            }

            private void Parser_ModuleIDDefinition(Microsoft.Diagnostics.Tracing.Parsers.ETWClrProfiler.ModuleIDDefintionArgs data)
            {
                var moduleID = data.ModuleID.ToString();
                var moduleName = data.Path;

                //if (_moduleNameDict.ContainsKey(moduleID))
                //    throw new System.Exception($"Duplicate module ID found. Module ID: {moduleID} Module Name: {moduleName}");
                _moduleNameDict[moduleID] = moduleName;
            }

            private void Parser_ClassIDDefinition(Microsoft.Diagnostics.Tracing.Parsers.ETWClrProfiler.ClassIDDefintionArgs data)
            {

                var classID = data.ClassID.ToString();
                var className = data.Name;
                var moduleID = data.ModuleID.ToString();

                if (moduleID != "0")
                {
                    //if (_className_classIDDict.ContainsKey(classID) || _objectModuleDict.ContainsKey(classID))
                    //    throw new System.Exception($"Duplicate class ID found. Class ID: {classID} Class Name: {className}");
                    _className_classIDDict[className] = classID;
                    _objectModuleDict[classID] = moduleID;
                }

                else
                {
                    if (!className.EndsWith("[]"))
                    {
                        // lazy messy way to do handle commas. fix later
                        className = className.Replace(",", "");
                        if (!className.EndsWith("[]"))
                            throw new System.Exception($"Cannot find module for class {className}.");
                    }
                    var fixedClassName = className;
                    while (fixedClassName.EndsWith("[]"))
                    {
                        fixedClassName = fixedClassName.Substring(0, fixedClassName.Length - 2);
                    }
                    string fixedClassID;

                    if (!_className_classIDDict.TryGetValue(fixedClassName, out fixedClassID))
                    {
                        throw new System.Exception($"Cannot find class ID for class {fixedClassName}");
                    }

                    if(!_objectModuleDict.TryGetValue(fixedClassID, out moduleID))
                        throw new System.Exception($"Cannot find module for class {className}.");
                    _objectModuleDict[classID] = moduleID;
                }
            }

            private void Parser_ObjectAllocated(Microsoft.Diagnostics.Tracing.Parsers.ETWClrProfiler.ObjectAllocatedArgs data)
            {
                if (_context.IsTestEvent(data))
                {
                    var classID = data.ClassID.ToString();
                    var moduleID = _objectModuleDict[classID];
                    var moduleName = _moduleNameDict[moduleID];
                    var size = data.Size;
                    _objects.addItem(moduleName, size);
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
