using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace xunit.performance.run
{
    class Program
    {
        static int Usage()
        {
            Console.Error.WriteLine(
                "usage: xunit.performance.analysis <etlPaths> [-compare \"baselineRunId\" \"comparisonRunId\"]  -xml <output.xml>");
            return 1;
        }

        static int Main(string[] args)
        {
            var etlPaths = new List<string>();
            var comparisonIds = new List<Tuple<string, string>>();
            var xmlOutputPath = (string)null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-") || args[i].StartsWith("/"))
                {
                    string switchName = args[i].Substring(1).ToLowerInvariant();
                    switch (switchName)
                    {
                        case "compare":
                            if (++i >= args.Length)
                                return Usage();
                            string baseline = args[i];
                            if (++i >= args.Length)
                                return Usage();
                            string comparison = args[i];
                            comparisonIds.Add(Tuple.Create(baseline, comparison));
                            break;

                        case "xml":
                            xmlOutputPath = args[i];
                            break;

                        default:
                            return Usage();
                    }
                }
                else
                {
                    bool foundFile = false;
                    foreach (var file in ExpandFilePath(args[i]))
                    {
                        if (file.EndsWith(".etl", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".etl.zip", StringComparison.OrdinalIgnoreCase))
                        {
                            foundFile = true;
                            etlPaths.Add(file);
                        }
                        else
                        {
                            Console.Error.WriteLine($"{file}' is not a .etl or .etl.zip file.");
                            return 1;
                        }
                    }
                    if (!foundFile)
                    {
                        Console.Error.WriteLine($"The path '{args[i]}' could not be found.");
                        return 1;
                    }
                }
            }

            if (etlPaths.Count == 0)
                return Usage();

            var allIterations = ParseEtlFiles(etlPaths);

            foreach (var iteration in allIterations)
            {
                Console.WriteLine($"{iteration.TestName},{iteration.Duration},{iteration.GCCount}");
            }

            return 0;
        }


        static IEnumerable<string> ExpandFilePath(string path)
        {
            if (File.Exists(path))
            {
                yield return path;
            }
            else if (Directory.Exists(path))
            {
                foreach (var file in Directory.EnumerateFiles(path, "*.etl"))
                    yield return file;

                foreach (var file in Directory.EnumerateFiles(path, "*.etl.zip"))
                    yield return file;
            }
        }

        class TestIterationResult
        {
            public string EtlPath;
            public string RunId;
            public string TestName;
            public int TestIteration;
            public DateTime StartTime;
            public double RelativeStartMilliseconds;
            public double RelativeStopMilliseconds;
            public double Duration => RelativeStopMilliseconds - RelativeStartMilliseconds;
            public int GCCount;

            public HashSet<int> tempProcessIds = new HashSet<int>(); // process IDs active for this iteration; used only while parsing.
        }

        static IEnumerable<TestIterationResult> ParseEtlFiles(IEnumerable<string> etlPaths)
        {
            return 
                from path in etlPaths.AsParallel()
                from result in ParseOneEtlFile(path)
                select result;
        }

        static IEnumerable<TestIterationResult> ParseOneEtlFile(string path)
        {
            Console.WriteLine($"Parsing {path}");

            List<TestIterationResult> results = new List<TestIterationResult>();
            using (var source = new ETWTraceEventSource(path))
            {
                if (source.EventsLost > 0)
                    throw new Exception($"Events were lost in trace '{path}'");

                MicrosoftXunitBenchmarkTraceEventParser benchmarkParser = new MicrosoftXunitBenchmarkTraceEventParser(source);

                Dictionary<string, TestIterationResult> currentIterations = new Dictionary<string, TestIterationResult>();

                benchmarkParser.BenchmarkExecutionStart += args =>
                {
                    var currentIteration = new TestIterationResult();
                    currentIteration.EtlPath = path;
                    currentIteration.RunId = args.RunId;
                    currentIteration.TestName = args.BenchmarkName;
                    currentIteration.TestIteration = args.Iteration;
                    currentIteration.StartTime = args.TimeStamp;
                    currentIteration.RelativeStartMilliseconds = args.TimeStampRelativeMSec;
                    currentIteration.tempProcessIds.Add(args.ProcessID);

                    currentIterations[args.RunId] = currentIteration;
                };

                benchmarkParser.BenchmarkExecutionStop += args =>
                {
                    TestIterationResult currentIteration = currentIterations[args.RunId];
                    currentIteration.RelativeStopMilliseconds = args.TimeStampRelativeMSec;

                    currentIterations.Remove(args.RunId);
                    currentIteration.tempProcessIds = null;
                    results.Add(currentIteration);                                        
                };

                source.Kernel.ProcessStart += args =>
                {
                    foreach (var currentIteration in currentIterations.Values)
                        if (currentIteration.tempProcessIds.Contains(args.ParentID))
                            currentIteration.tempProcessIds.Add(args.ProcessID);
                };

                source.Kernel.ProcessStop += args =>
                {
                    foreach (var currentIteration in currentIterations.Values)
                        currentIteration.tempProcessIds.Remove(args.ProcessID);
                };

                source.Clr.GCStart += args =>
                {
                    foreach (var currentIteration in currentIterations.Values)
                        if (currentIteration.tempProcessIds.Contains(args.ProcessID))
                            currentIteration.GCCount++;
                };

                source.Process();
            }

            return results;
        }
    }
}
