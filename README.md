# xunit.performance

Build | Status
------------ | -------------
Release | [![Build Status](https://ci.dot.net/job/Microsoft_xunit-performance/job/master/job/LinuxFlow_Ubuntu_release/badge/icon)](https://ci.dot.net/job/Microsoft_xunit-performance/job/master/job/LinuxFlow_Ubuntu_release/)
Debug | [![Build Status](https://ci.dot.net/job/Microsoft_xunit-performance/job/master/job/LinuxFlow_Ubuntu_debug/badge/icon)](https://ci.dot.net/job/Microsoft_xunit-performance/job/master/job/LinuxFlow_Ubuntu_debug/)

Provides extensions over xUnit to author performance tests.

## Authoring benchmarks

1. Create a new class library project
2. Add a reference to the latest [xunit.performance.api.dll](https://dotnet.myget.org/feed/dotnet-core/package/nuget/xunit.performance.api)
3. Add a reference to the latest [Microsoft.Diagnostics.Tracing.TraceEvent](https://www.nuget.org/packages/Microsoft.Diagnostics.Tracing.TraceEvent)
    (It deploys native libraries needed to merge the \*.etl files)
4. Tag your test methods with [Benchmark] instead of [Fact]
5. Make sure that each [Benchmark]-annotated test contains a loop of this form:

```csharp
[Benchmark]
void TestMethod()
{
    // Any per-test-case setup can go here.
    foreach (var iteration in Benchmark.Iterations)
    {
        // Any per-iteration setup can go here.
        using (iteration.StartMeasurement())
        {
            // Code to be measured goes here.
        }
        // ...per-iteration cleanup
    }
    // ...per-test-case cleanup
}
```

The simplest possible benchmark is therefore:

```csharp
[Benchmark]
void EmptyBenchmark()
{
    foreach (var iteration in Benchmark.Iterations)
        using (iteration.StartMeasurement())
            ; //do nothing
}
```

Which can also be written as:

```csharp
[Benchmark]
void EmptyBenchmark()
{
    Benchmark.Iterate(() => { /*do nothing*/ });
}
```

In addition, you can add inner iterations to the code to be measured.

1. Add the for loop using Benchmark.InnerIterationCount as the number of loop iterations
2. Specify the value of InnerIterationCount using the [Benchmark] attribute

```csharp
[Benchmark(InnerIterationCount=500)]
void TestMethod()
{
    // The first iteration is the "warmup" iteration, where all performance
    // metrics are discarded. Subsequent iterations are measured.
    foreach (var iteration in Benchmark.Iterations)
        using (iteration.StartMeasurement())
            // Inner iterations are recommended for fast running benchmarks
            // that complete very quickly (microseconds). This ensures that
            // the benchmark code runs long enough to dominate the harness's
            // overhead.
            for (int i=0; i<Benchmark.InnerIterationCount; i++)
                // test code here
}
```

If you need to execute different permutation of the same benchmark, then you can use this approach:

```csharp
public static IEnumerable<object[]> InputData()
{
    var args = new string[] { "foo", "bar", "baz" };
    foreach (var arg in args)
        // Currently, the only limitation of this approach is that the
        // types passed to the [Benchmark]-annotated test must be serializable.
        yield return new object[] { new string[] { arg } };
}

// NoInlining prevents aggressive optimizations that
// could render the benchmark meaningless
[MethodImpl(MethodImplOptions.NoInlining)]
private static string FormattedString(string a, string b, string c, string d)
{
    return string.Format("{0}{1}{2}{3}", a, b, c, d);
}

// This benchmark will be executed 3 different times,
// with { "foo" }, { "bar" }, and { "baz" } as args.
[MeasureGCCounts]
[Benchmark(InnerIterationCount = 10)]
[MemberData(nameof(InputData))]
public static void TestMultipleStringInputs(string[] args)
{
    foreach (BenchmarkIteration iter in Benchmark.Iterations)
    {
        using (iter.StartMeasurement())
        {
            for (int i = 0; i < Benchmark.InnerIterationCount; i++)
            {
                FormattedString(args[0], args[0], args[0], args[0]);
            }
        }
    }
}
```

## Creating a simple harness to execute the API

### Option #1: Creating a self containing harness + benchmark

```csharp
using Microsoft.Xunit.Performance;
using Microsoft.Xunit.Performance.Api;
using System.Reflection;

public class Program
{
    public static void Main(string[] args)
    {
        using (XunitPerformanceHarness p = new XunitPerformanceHarness(args))
        {
            string entryAssemblyPath = Assembly.GetEntryAssembly().Location;
            p.RunBenchmarks(entryAssemblyPath);
        }
    }

    [Benchmark(InnerIterationCount=10000)]
    public void TestBenchmark()
    {
        foreach(BenchmarkIteration iter in Benchmark.Iterations)
        {
            using(iter.StartMeasurement())
            {
                for(int i=0; i<Benchmark.InnerIterationCount; i++)
                {
                    string.Format("{0}{1}{2}{3}", "a", "b", "c", "d");
                }
            }
        }
    }
}

```

### Option #2: Creating a harness that iterates through a list of .NET assemblies containing the benchmarks.

```csharp
using System.IO;
using System.Reflection;
using Microsoft.Xunit.Performance.Api;

namespace SampleApiTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using (var harness = new XunitPerformanceHarness(args))
            {
                foreach(var testName in GetTestNames())
                {
                    // Here, the example assumes that the list of .NET
                    // assemblies are dropped side-by-side with harness
                    // (the current executing assembly)
                    var currentDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                    var assemblyPath = Path.Combine(currentDirectory, $"{testName}.dll");

                    // Execute the benchmarks, if any, in this assembly.
                    harness.RunBenchmarks(assemblyPath);
                }
            }
        }

        private static string[] GetTestNames()
        {
            return new [] {
                "Benchmarks",
                "System.Binary.Base64.Tests",
                "System.Text.Primitives.Performance.Tests",
                "System.Slices.Tests"
            };
        }
    }
}
```

## Command line options to control the collection of metrics

```
--perf:collect [metric1[+metric2[+...]]]

    default
        Set by the test author (This is the default behavior if no option is specified. It will also enable ETW to capture some of the Microsoft-Windows-DotNETRuntime tasks).

    stopwatch
        Capture elapsed time using a Stopwatch (It does not require ETW).

    BranchMispredictions|CacheMisses|InstructionRetired
        These are performance metric counters and require ETW.

    gcapi
        It currently enable "Allocation Size on Benchmark Execution Thread" and it is currently available through ETW.

Examples
  --perf:collect default
    Collect metrics specified in the test source code by using xUnit Performance API attributes

  --perf:collect BranchMispredictions+CacheMisses+InstructionRetired
    Collects BranchMispredictions, CacheMisses, and InstructionRetired PMC metrics

  --perf:collect stopwatch
    Collects the benchmark elapsed time (If this is the only specified metric on the command line, then no ETW will be captured)

  --perf:collect default+BranchMispredictions+CacheMisses+InstructionRetired+gcapi
    '+' implies union of all specified options
```

## Supported metrics

Currently, the API collect the following data \*:

Metric                                | Type                                     | Description
------------------------------------- | ---------------------------------------- | -------------------------------------------------------------------------------------------------------------------
**Allocated Bytes in Current Thread** | GC API call                              | Calls `GC.GetAllocatedBytesForCurrentThread` around the benchmark (Enabled if available on the target .NET runtime)
**Branch Mispredictions**             | Performance Monitor Counter              | Enabled if the collection option `BranchMispredictions` is specified and the counter is available on the machine <br/>(It requires to run as Administrator)
**Cache Misses**                      | Performance Monitor Counter              | Enabled if the collection option `CacheMisses` is specified and the counter is available on the machine <br/>(It requires to run as Administrator)
**Duration**                          | Benchmark execution time in milliseconds | Always enabled
**GC Allocations** \*\*               | GC trace event                           | Use the `[MeasureGCAllocations]` attribute in the source code
**GC Count** \*\*                     | GC trace event                           | Use the `[MeasureGCCounts]` attribute in the source code
**Instructions Retired** \*\*         | Performance Monitor Counter              | Enabled if the collection option `InstructionRetired` is specified or the `[MeasureInstructionsRetired]` attribute is used in the source code, and the counter is available on the machine<br/>(It requires to run as Administrator)

\* The default metrics are subject to change, and we are currently working on enabling more metrics and adding support to have more control around the metrics being captured.
\*\* These attributes can be overriden using the `--perf:collect` option

## Collected data

Currently the API generates different output files with the collected data:

Format | Data
:----: | ----
  csv  | File contaning statistics of the collected metrics
  etl  | Trace file (Windows only)
  md   | Markdown file with statistics rendered as a table (github friendly)
  xml  | Serialized raw data of all of the tests with their respective metrics

## Authoring Scenario-based Benchmarks

A Scenario-based benchmark is one that runs in a separate process. Therefore, in order to author this kind of test you need to provide an executable, as well as some information for xunit-performance to run. You are responsible for all the measurements, not only to decide what to measure but also how to get the actual numbers.

1. Create a new Console Application project
2. Add a reference to the "xUnit" NuGet package
3. Add a reference to the latest [xunit.performance.api.dll](https://dotnet.myget.org/feed/dotnet-core/package/nuget/xunit.performance.api)
4. Define PreIteration and PostIteration delegates
5. Define PostRun Delegate
6. In the main function of your project, specify a ProcessStartInfo for your executable and provide it to xunit-performance.

You have the option of doing all the setup for your executable (downloading a repository, building, doing a restore, etc.) or you can indicate the location of your pre-compiled executable.

PreIteration and PostIteration are delegates that will be called once per run of your app, before and after, respectively.
PostRun is a delegate that will be called after all the iterations are complete, and should return an object of type ScenarioBenchmark filled with your tests and metrics names, as well as the numbers you obtained.

### Example

In this example, HelloWorld is a simple program that does some stuff, measures how much time it spent, and outputs this number to a txt file. The test author has decided that it only has one Test, called "Doing Stuff", and this test has only one metric to measure, "Execution Time".

The authoring might look something like this:

```csharp
private const double TimeoutInMilliseconds = 20000;
private const int NumberOfIterations = 10;
private static int s_iteration = 0;
private static double[] s_startupTimes = new double[NumberOfIterations];
private static double[] s_requestTimes = new double[NumberOfIterations];
private static ScenarioConfiguration s_scenarioConfiguration = new ScenarioConfiguration(TimeoutInMilliseconds, NumberOfIterations);

public static void Main(string[] args)
{
  // Optional setup steps. e.g.)
  //  Clone repository
  //  Build benchmark

  using (var h = new XunitPerformanceHarness(args))
  {
    var startInfo = new ProcessStartInfo() {
      FileName = "helloWorld.exe"
    };

    h.RunScenario(
      startInfo,
      PreIteration,
      PostIteration,
      PostRun,
      s_scenarioConfiguration);
  }
}

private static void PreIteration()
{
  // Optional pre benchmark iteration steps.
}

private static void PostIteration()
{
  // Optional post benchmark iteration steps. For example:
  //  - Read measurements from txt file
  //  - Save measurements to buffer (e.g. s_startupTimes and s_requestTimes)
  ++s_iteration;
}

// After all iterations, we create the ScenarioBenchmark object, and we add
// only one test with one metric. Then we add one Iteration for each iteration
// that run.
private static ScenarioBenchmark PostRun()
{
  var scenarioBenchmark = new ScenarioBenchmark("MusicStore") {
    Namespace = "JitBench"
  };

  var startup = new ScenarioTestModel("Startup");
  scenarioBenchmark.Tests.Add(startup);

  var request = new ScenarioTestModel("Request Time");
  scenarioBenchmark.Tests.Add(request);

  // Add the measured metrics to the startup test
  startup.Performance.Metrics.Add(new MetricModel {
    Name = "ExecutionTime",
    DisplayName = "Execution Time",
    Unit = "ms"
  });

  // Add the measured metrics to the request test
  request.Performance.Metrics.Add(new MetricModel {
    Name = "ExecutionTime",
    DisplayName = "Execution Time",
    Unit = "ms"
  });

  for (int i = 0; i < s_scenarioConfiguration.Iterations; ++i)
  {
      var startupIteration = new IterationModel {
        Iteration = new Dictionary<string, double>()
      };
      startupIteration.Iteration.Add("ExecutionTime", s_startupTimes[i]);
      startup.Performance.IterationModels.Add(startupIteration);

      var requestIteration = new IterationModel {
        Iteration = new Dictionary<string, double>()
      };
      requestIteration.Iteration.Add("ExecutionTime", s_requestTimes[i]);
      request.Performance.IterationModels.Add(requestIteration);
  }

  return scenarioBenchmark;
}
```

Once you create an instance of the XunitPerformanceHarness, it comes with a configuration object of type ScenarioConfiguration, which has default values that you can edit to properly apply to your test requirements.

```csharp
public class ScenarioConfiguration
{
  public int Iterations { get; }
  public TimeSpan TimeoutPerIteration { get; }
}
```

## Controlling the order of executed benchmarks

To control the order of benchmarks executed within a type you need to use an existing `xunit` feature. All you have to do is to implement a type which implements `ITestCaseOrderer` interface and configure it by using `[TestCaseOrderer]` attribute.

Example:

```cs
public class DefaultTestCaseOrderer : ITestCaseOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases) where TTestCase : ITestCase
        => testCases.OrderBy(test => test.DisplayName); // OrderBy provides stable sort ([msdn](https://msdn.microsoft.com/en-us/library/bb534966.aspx))
}

[assembly: TestCaseOrderer("namespace.OrdererTypeName", "assemblyName")]
```

**Note:** Please make sure that you have provided full type name (with namespace) and the correct assembly name. Wrong configuration ends up with a **silent error**.

