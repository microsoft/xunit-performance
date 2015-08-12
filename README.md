# xunit.performance
Provides extensions over xUnit to author performance tests.

## Authoring benchmarks

1. Create a new class library project
2. Add a reference to the "xUnit" NuGet package
3. Add a reference to xunit.performance.dll
4. Tag your test methods with [Benchmark] instead of [Fact]

## Running benchmarks

The normal xUnit runners will run Benchmarks as normal unit tests.  The test execution times reported in the normal xUnit test results are for a single iteration, and so do not tell you much about the methods' performance.

To collect more detailed data, use xunit.performance.run.exe:

> xunit.performance.run MyTests.dll -runner xunit.console.exe -runName MyRun1234

This will produce a file named MyRun1234.etl in the current directory.

## Analyzing test results

Use xunit.performance.analysis.exe:

> xunit.performance.analysis MyRun1234

