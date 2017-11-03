using Microsoft.Xunit.Performance;
using Microsoft.Xunit.Performance.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;


namespace simpleharness
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using (var harness = new XunitPerformanceHarness(args))
            {
                Console.Out.WriteLine($"[{DateTime.Now}] Harness start");
                TestDir(harness);
                Console.Out.WriteLine($"[{DateTime.Now}] Harness stop");
            }
        }

        static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
        const int Iterations = 10;

        private static void TestDir(XunitPerformanceHarness harness)
        {
            string commandName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dir" : "ls";

            var testModel = new ScenarioTestModel(commandName);

            testModel.Performance.Metrics.Add(new MetricModel
            {
                Name = "ExecutionTime",
                DisplayName = "Execution Time",
                Unit = "ms"
            });

            void PreIteration(ScenarioTest scenarioTest)
            {

            }

            void PostIteration(ScenarioExecutionResult scenarioExecutionResult)
            {
                var elapsed = scenarioExecutionResult.ProcessExitInfo.ExitTime - scenarioExecutionResult.ProcessExitInfo.StartTime;

                var iteration = new IterationModel
                {
                    Iteration = new Dictionary<string, double>()
                };
                iteration.Iteration.Add(testModel.Performance.Metrics[0].Name, elapsed.TotalMilliseconds);
                testModel.Performance.IterationModels.Add(iteration);
            }

            void PostRun(ScenarioBenchmark scenario)
            {

            }

            ProcessStartInfo processToMeasure;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                processToMeasure = new ProcessStartInfo("cmd", $"/c {commandName}");
            }
            else
            {
                processToMeasure = new ProcessStartInfo(commandName);
            }            

            var scenarioTestConfiguration = new ScenarioTestConfiguration(Timeout, processToMeasure);
            scenarioTestConfiguration.Iterations = Iterations;
            scenarioTestConfiguration.PreIterationDelegate = PreIteration;
            scenarioTestConfiguration.PostIterationDelegate = PostIteration;
            scenarioTestConfiguration.Scenario = new ScenarioBenchmark("ExecuteCommand");
            scenarioTestConfiguration.Scenario.Tests.Add(testModel);
            scenarioTestConfiguration.TestName = commandName;

            harness.RunScenario(scenarioTestConfiguration, PostRun);
        }
    }
}