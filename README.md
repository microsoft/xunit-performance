# xunit.performance
Provides extensions over xUnit to author performance tests.

## Authoring benchmarks

1. Create a new class library project
2. Add a reference to the "xUnit" NuGet package
3. Add a reference to xunit.performance.dll
4. Tag your test methods with [Benchmark] instead of [Fact]

## Running benchmarks

The normal xUnit runners will run Benchmarks.  The difference between a Benchmark and a Fact is that a Benchmark will be run multiple times, until the test framework decides it's got enough data to make an accurate assessment of the performance of the method.  The test execution times reported in the normal xUnit test results are an aggregate of all iterations, and so do not tell you much about the time taken by each individual iteration.

To collect more detailed data, set some environment variables prior to running the tests:

> set XUNIT_PERFORMANCE_RUN_ID=myRunId

> set XUNIT_PERFORMANCE_ETL_PATH=c:\someDir\myRun.etl

The first variable specifies a name to identify this particular test run.  This will be useful later when analyzing the test results.  The second gives the path to a .etl file to receive ETW data from the run.  This is where all the detailed test metrics will be stored.

(Note: the use of environment variables for this purpose is a temporary situation; we're working on a better workflow.)


## Analyzing test results

Use xunit.performance.analysis.exe.  Currently this is *very* basic; you give it a list of .etl files, and it prints some raw data about each test iteration to the console.  This is a work in progress.

