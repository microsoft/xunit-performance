# xunit.performance

Build | Status
------------ | -------------
Release | [![Build Status](http://dotnet-ci.cloudapp.net/buildStatus/icon?job=microsoft_xunit-performance_release)](http://dotnet-ci.cloudapp.net/job/microsoft_xunit-performance_release/)
Debug | [![Build Status](http://dotnet-ci.cloudapp.net/buildStatus/icon?job=microsoft_xunit-performance_debug)](http://dotnet-ci.cloudapp.net/job/microsoft_xunit-performance_debug)

Provides extensions over xUnit to author performance tests.

## Authoring benchmarks

1. Create a new class library project
2. Add a reference to the "xUnit" NuGet package
3. Add a reference to xunit.performance.dll
4. Tag your test methods with [Benchmark] instead of [Fact]

Each [Benchmark]-annotated test must contain a loop of this form:

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

This may also be written as:

```csharp
[Benchmark]
void EmptyBenchmark()
{
  Benchmark.Iterate(() => { /*do nothing*/ });
}
```

The first iteration is the "warmup" iteration; all performance metrics are discarded by the result analyzer.  Subsequent iterations are measured. 

## Running benchmarks

The normal xUnit runners will run Benchmarks as normal unit tests.  The test execution times reported in the normal xUnit test results are for a single iteration, and so do not tell you much about the methods' performance.

To collect more detailed data, use xunit.performance.run.exe:

> xunit.performance.run MyTests.dll -runner xunit.console.exe -runid MyRun1234

This will produce files named MyRun1234.etl and MyRun1234.xml in the current directory.  MyRun1234.xml contains the detailed test results (including all performance metrics).

## Analyzing test results

Use xunit.performance.analysis.exe:

> xunit.performance.analysis MyRun1234

