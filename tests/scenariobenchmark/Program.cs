using Microsoft.Xunit.Performance.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace simpleharness
{
    public static class Program
    {
        const int Iterations = 10;

        static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        public static void Main(string[] args)
        {
            using (var harness = new XunitPerformanceHarness(args))
            {
                Console.Out.WriteLine($"[{DateTime.Now}] Harness start");
                TestDir(harness);
                Console.Out.WriteLine($"[{DateTime.Now}] Harness stop");
            }
        }

        static void TestDir(XunitPerformanceHarness harness)
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
                processToMeasure = new ProcessStartInfo("cmd.exe", $"/c {commandName}");
            }
            else
            {
                processToMeasure = new ProcessStartInfo(commandName);
            }

            processToMeasure.RedirectStandardError = true;
            processToMeasure.RedirectStandardOutput = true;
            processToMeasure.UseShellExecute = false;

            var scenarioTestConfiguration = new ScenarioTestConfiguration(Timeout, processToMeasure)
            {
                Iterations = Iterations,
                PreIterationDelegate = PreIteration,
                PostIterationDelegate = PostIteration,
                Scenario = new ScenarioBenchmark("ExecuteCommand")
            };
            scenarioTestConfiguration.Scenario.Tests.Add(testModel);
            scenarioTestConfiguration.TestName = commandName;

            harness.RunScenario(scenarioTestConfiguration, PostRun);
        }
    }
}