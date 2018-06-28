using Microsoft.Xunit.Performance;
using Microsoft.Xunit.Performance.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

[assembly: MeasureInstructionsRetired]

namespace simpleharness
{
    [MeasureGCAllocations]
    public static class Program
    {
        public static IEnumerable<object[]> InputData()
        {
            var args = new string[] { "FOO", "\u03C3", "x\u0305" };
            foreach (var arg in args)
                yield return new object[] { new string[] { arg } };
        }

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

        [MeasureGCCounts]
        [Benchmark(InnerIterationCount = 10, Skip = "This is a duplicated benchmark that needs to be skipped.")]
        [MemberData(nameof(InputData))]
        public static void TestMultipleStringInputs2(string[] args)
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        static string FormattedString(string a, string b, string c, string d) => string.Format("{0}{1}{2}{3}", a, b, c, d);

        static void Main(string[] args)
        {
            using (var p = new XunitPerformanceHarness(args))
            {
                Console.Out.WriteLine($"[{DateTime.Now}] Harness start");
                p.RunBenchmarks(Assembly.GetEntryAssembly().Location);
                Console.Out.WriteLine($"[{DateTime.Now}] Harness stop");
            }
        }

        [MeasureGCAllocations]
        public sealed class Type_1
        {
            [MeasureGCCounts]
            [Benchmark(InnerIterationCount = 10000)]
            public void TestBenchmark()
            {
                foreach (BenchmarkIteration iter in Benchmark.Iterations)
                {
                    using (iter.StartMeasurement())
                    {
                        for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        {
                            string.Format("{0}{1}{2}{3}", "a", "b", "c", "d");
                        }
                    }
                }
            }

            [Fact]
            public void TestFact()
            {
                foreach (BenchmarkIteration iter in Benchmark.Iterations)
                {
                    using (iter.StartMeasurement())
                    {
                        for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        {
                            string.Format("{0}{1}{2}{3}", "a", "b", "c", "d");
                        }
                    }
                }
            }
        }

        public sealed class Type_2
        {
            [Benchmark(InnerIterationCount = 1)]
            public static void RandomAccess()
            {
                foreach (BenchmarkIteration iter in Benchmark.Iterations)
                {
                    using (iter.StartMeasurement())
                    {
                        for (int i = 0; i < Benchmark.InnerIterationCount; ++i)
                        {
                            MemoryAccessPerformance();
                        }
                    }
                }
            }

            [Benchmark(InnerIterationCount = 10000)]
            public static void ShufflingDeckOfCard()
            {
                foreach (BenchmarkIteration iter in Benchmark.Iterations)
                {
                    using (iter.StartMeasurement())
                    {
                        for (int i = 0; i < Benchmark.InnerIterationCount; ++i)
                        {
                            BranchPredictionPerformance(i);
                        }
                    }
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static IEnumerable<int> BranchPredictionPerformance(int seed)
            {
                const int nCards = 52;
                var deck = new List<int>(Enumerable.Range(0, nCards));
                var rnd = new Random((int)DateTime.Now.Ticks + seed);

                for (int i = 0; i < deck.Count; ++i)
                {
                    var pos = rnd.Next(nCards);
                    if (pos % 3 != 0)
                        pos = rnd.Next(nCards);
                    var temp = deck[i];
                    deck[i] = deck[pos];
                    deck[pos] = temp;
                }

                return deck;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static double MemoryAccessPerformance()
            {
                var doubles = new double[8 * 1024 * 1024];
                for (int i = 0; i < doubles.Length; i += 100)
                    doubles[i] = 2.0;
                for (int i = 0; i < doubles.Length; i += 200)
                    doubles[i] *= 3.0;
                for (int i = 0; i < doubles.Length; i += 400)
                    doubles[i] *= 5.0;
                for (int i = 0; i < doubles.Length; i += 800)
                    doubles[i] *= 7.0;
                for (int i = 0; i < doubles.Length; i += 1600)
                    doubles[i] *= 11.0;
                return doubles.Average();
            }
        }
    }
}