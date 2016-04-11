// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

[MeasureGCCounts]
[MeasureGCAllocations]
public static class StringThroughput
{
    #region helpers
    private static IEnumerable<object[]> MakeArgs(params object[] args)
    {
        return args.Select(arg => new object[] { arg });
    }

    private static IEnumerable<object[]> MakePermutations(IEnumerable<object[]> args1, IEnumerable<object[]> args2)
    {
        foreach (var arg1 in args1)
            foreach (var arg2 in args2)
                yield return new[] { arg1[0], arg2[0] };
    }
    #endregion

    public static string i1 = "ddsz dszdsz \t  dszdsz  a\u0300\u00C0 \t Te st \u0400Te \u0400st\u0020\u00A0\u2000\u2001\u2002\u2003\u2004\u2005\t Te\t \tst \t\r\n\u0020\u00A0\u2000\u2001\u2002\u2003\u2004\u2005";
    public static int counter;

    [Benchmark]
    public static void BoundCheckHoist()
    {
        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                {
                    int strLength = i1.Length;

                    for (int j = 0; j < strLength; j++)
                    {
                        counter += i1[j];
                    }
                }
            }
        }
    }

    [Benchmark]
    public static void LengthHoisting()
    {
        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                {
                    for (int j = 0; j < i1.Length; j++)
                    {
                        counter += i1[j];
                    }
                }
            }
        }
    }

    [Benchmark]
    public static void PathLength()
    {
        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                {
                    for (int j = 0; j < i1.Length; j++)
                    {
                        counter += GetStringCharNoInline(i1, j);
                    }
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static char GetStringCharNoInline(string str, int index)
    {
        return str[index];
    }

    public static IEnumerable<object[]> CaseStrings => MakeArgs(
        String.Empty,
        " ",
        "TeSt",
        "TEST",
        "test",
        "I think Turkish i \u0131s TROUBL\u0130NG",
        "dzsdzsDDZSDZSDZSddsz",
        "a\u0300\u00C0A\u0300",
        "Foo\u0400Bar",
        "a\u0020a\u00A0A\u2000a\u2001a\u2002A\u2003a\u2004a\u2005a",
        "\u4e33\u4e65 Testing... \u4EE8"
    );


    [Benchmark]
    [MemberData(nameof(CaseStrings))]
    public static void ToUpper(string s)
    {
        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    s.ToUpper();
    }

    [Benchmark]
    [MemberData(nameof(CaseStrings))]
    public static void ToUpperInvariant(string s)
    {
        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    s.ToUpperInvariant();
    }

    public static IEnumerable<object[]> TrimStrings => MakeArgs(
            String.Empty,
            " ",
            "   ",
            "Test",
            " Test",
            "Test ",
            " Te st  ",
            "\u0020\u00A0\u2000\u2001\u2002\u2003\u2004\u2005\t Te\t \tst \t\r\n\u0020\u00A0\u2000\u2001\u2002\u2003\u2004\u2005",
            " \u0400Te \u0400st",
            " a\u0300\u00C0",
            " a \u0300 \u00C0 ",
            "     ddsz dszdsz \t  dszdsz  \t        "
        );

    public static IEnumerable<object[]> TrimChars => MakeArgs(
        null,
        new char[] { 'T' },
        new char[] { 'T', 'T', 'T', 'T', 'T' },
        new char[] { 'a' },
        new char[] { 'T', (char)192 },
        new char[] { ' ', (char)8197 },
        new char[] { "\u0400"[0] },
        new char[] { '1', 'a', ' ', '0', 'd', 'e', 's', 't', "\u0400"[0] }
        );

    public static IEnumerable<object[]> TrimStringsAndChars = MakePermutations(TrimStrings, TrimChars);

    [Benchmark]
    [MemberData(nameof(TrimStrings))]
    public static void Trim(string s)
    {
        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    s.Trim();
    }

    [Benchmark]
    [MemberData(nameof(TrimStringsAndChars))]
    public static void TrimCharArr(string s, char[] c)
    {
        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    s.Trim(c);
    }

    [Benchmark]
    [MemberData(nameof(TrimStringsAndChars))]
    public static void TrimStart(string s, char[] c)
    {
        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    s.TrimStart(c);
    }

    [Benchmark]
    [MemberData(nameof(TrimStringsAndChars))]
    public static void TrimEnd(string s, char[] c)
    {
        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    s.TrimEnd(c);
    }

    #region more tests
    //[Benchmark]
    //public static void Insert()
    //{
    //    t1.Insert(0, t2); t2.Insert(1, t3); t3.Insert(1, t4); t4.Insert(2, t5); t5.Insert(2, t6); t6.Insert(2, t7);
    //    t7.Insert(3, t8); t8.Insert(3, t9); t9.Insert(3, tA); tA.Insert(3, tB); tB.Insert(4, tC); tC.Insert(5, t1);
    //}

    //static char c1 = ' ';
    //static char c2 = 'T';
    //static char c3 = 'd';
    //static char c4 = 'a';
    //static char c5 = (char)192;
    //static char c6 = (char)8197;
    //static char c7 = "\u0400"[0];
    //static char c8 = '\t';
    //static char c9 = (char)768;

    //[Benchmark]
    //public static void ReplaceChar()
    //{
    //    t1.Replace(c1, c2); t1.Replace(c2, c3); t1.Replace(c3, c4); t1.Replace(c4, c5); t1.Replace(c5, c6); t1.Replace(c6, c7); t1.Replace(c7, c8); t1.Replace(c8, c9); t1.Replace(c9, c1);
    //    t2.Replace(c1, c2); t2.Replace(c2, c3); t2.Replace(c3, c4); t2.Replace(c4, c5); t2.Replace(c5, c6); t2.Replace(c6, c7); t2.Replace(c7, c8); t2.Replace(c8, c9); t2.Replace(c9, c1);
    //    t3.Replace(c1, c2); t3.Replace(c2, c3); t3.Replace(c3, c4); t3.Replace(c4, c5); t3.Replace(c5, c6); t3.Replace(c6, c7); t3.Replace(c7, c8); t3.Replace(c8, c9); t3.Replace(c9, c1);
    //    t4.Replace(c1, c2); t4.Replace(c2, c3); t4.Replace(c3, c4); t4.Replace(c4, c5); t4.Replace(c5, c6); t4.Replace(c6, c7); t4.Replace(c7, c8); t4.Replace(c8, c9); t4.Replace(c9, c1);
    //    t5.Replace(c1, c2); t5.Replace(c2, c3); t5.Replace(c3, c4); t5.Replace(c4, c5); t5.Replace(c5, c6); t5.Replace(c6, c7); t5.Replace(c7, c8); t5.Replace(c8, c9); t5.Replace(c9, c1);
    //    t6.Replace(c1, c2); t6.Replace(c2, c3); t6.Replace(c3, c4); t6.Replace(c4, c5); t6.Replace(c5, c6); t6.Replace(c6, c7); t6.Replace(c7, c8); t6.Replace(c8, c9); t6.Replace(c9, c1);
    //    t7.Replace(c1, c2); t7.Replace(c2, c3); t7.Replace(c3, c4); t7.Replace(c4, c5); t7.Replace(c5, c6); t7.Replace(c6, c7); t7.Replace(c7, c8); t7.Replace(c8, c9); t7.Replace(c9, c1);
    //    t8.Replace(c1, c2); t8.Replace(c2, c3); t8.Replace(c3, c4); t8.Replace(c4, c5); t8.Replace(c5, c6); t8.Replace(c6, c7); t8.Replace(c7, c8); t8.Replace(c8, c9); t8.Replace(c9, c1);
    //    t9.Replace(c1, c2); t9.Replace(c2, c3); t9.Replace(c3, c4); t9.Replace(c4, c5); t9.Replace(c5, c6); t9.Replace(c6, c7); t9.Replace(c7, c8); t9.Replace(c8, c9); t9.Replace(c9, c1);
    //    tA.Replace(c1, c2); tA.Replace(c2, c3); tA.Replace(c3, c4); tA.Replace(c4, c5); tA.Replace(c5, c6); tA.Replace(c6, c7); tA.Replace(c7, c8); tA.Replace(c8, c9); tA.Replace(c9, c1);
    //    tB.Replace(c1, c2); tB.Replace(c2, c3); tB.Replace(c3, c4); tB.Replace(c4, c5); tB.Replace(c5, c6); tB.Replace(c6, c7); tB.Replace(c7, c8); tB.Replace(c8, c9); tB.Replace(c9, c1);
    //    tC.Replace(c1, c2); tC.Replace(c2, c3); tC.Replace(c3, c4); tC.Replace(c4, c5); tC.Replace(c5, c6); tC.Replace(c6, c7); tC.Replace(c7, c8); tC.Replace(c8, c9); tC.Replace(c9, c1);
    //}

    //static string sc1 = "  ";
    //static string sc2 = "T";
    //static string sc3 = "dd";
    //static string sc4 = "a";
    //static string sc5 = "\u00C0";
    //static string sc6 = "a\u0300";
    //static string sc7 = "\u0400";
    //static string sc8 = "\u0300";
    //static string sc9 = "\u00A0\u2000";

    //[Benchmark]
    //public static void ReplaceString()
    //{
    //    t1.Replace(sc1, sc2); t1.Replace(sc2, sc3); t1.Replace(sc3, sc4); t1.Replace(sc4, sc5); t1.Replace(sc5, sc6); t1.Replace(sc6, sc7); t1.Replace(sc7, sc8); t1.Replace(sc8, sc9); t1.Replace(sc9, sc1);
    //    t2.Replace(sc1, sc2); t2.Replace(sc2, sc3); t2.Replace(sc3, sc4); t2.Replace(sc4, sc5); t2.Replace(sc5, sc6); t2.Replace(sc6, sc7); t2.Replace(sc7, sc8); t2.Replace(sc8, sc9); t2.Replace(sc9, sc1);
    //    t3.Replace(sc1, sc2); t3.Replace(sc2, sc3); t3.Replace(sc3, sc4); t3.Replace(sc4, sc5); t3.Replace(sc5, sc6); t3.Replace(sc6, sc7); t3.Replace(sc7, sc8); t3.Replace(sc8, sc9); t3.Replace(sc9, sc1);
    //    t4.Replace(sc1, sc2); t4.Replace(sc2, sc3); t4.Replace(sc3, sc4); t4.Replace(sc4, sc5); t4.Replace(sc5, sc6); t4.Replace(sc6, sc7); t4.Replace(sc7, sc8); t4.Replace(sc8, sc9); t4.Replace(sc9, sc1);
    //    t5.Replace(sc1, sc2); t5.Replace(sc2, sc3); t5.Replace(sc3, sc4); t5.Replace(sc4, sc5); t5.Replace(sc5, sc6); t5.Replace(sc6, sc7); t5.Replace(sc7, sc8); t5.Replace(sc8, sc9); t5.Replace(sc9, sc1);
    //    t6.Replace(sc1, sc2); t6.Replace(sc2, sc3); t6.Replace(sc3, sc4); t6.Replace(sc4, sc5); t6.Replace(sc5, sc6); t6.Replace(sc6, sc7); t6.Replace(sc7, sc8); t6.Replace(sc8, sc9); t6.Replace(sc9, sc1);
    //    t7.Replace(sc1, sc2); t7.Replace(sc2, sc3); t7.Replace(sc3, sc4); t7.Replace(sc4, sc5); t7.Replace(sc5, sc6); t7.Replace(sc6, sc7); t7.Replace(sc7, sc8); t7.Replace(sc8, sc9); t7.Replace(sc9, sc1);
    //    t8.Replace(sc1, sc2); t8.Replace(sc2, sc3); t8.Replace(sc3, sc4); t8.Replace(sc4, sc5); t8.Replace(sc5, sc6); t8.Replace(sc6, sc7); t8.Replace(sc7, sc8); t8.Replace(sc8, sc9); t8.Replace(sc9, sc1);
    //    t9.Replace(sc1, sc2); t9.Replace(sc2, sc3); t9.Replace(sc3, sc4); t9.Replace(sc4, sc5); t9.Replace(sc5, sc6); t9.Replace(sc6, sc7); t9.Replace(sc7, sc8); t9.Replace(sc8, sc9); t9.Replace(sc9, sc1);
    //    tA.Replace(sc1, sc2); tA.Replace(sc2, sc3); tA.Replace(sc3, sc4); tA.Replace(sc4, sc5); tA.Replace(sc5, sc6); tA.Replace(sc6, sc7); tA.Replace(sc7, sc8); tA.Replace(sc8, sc9); tA.Replace(sc9, sc1);
    //    tB.Replace(sc1, sc2); tB.Replace(sc2, sc3); tB.Replace(sc3, sc4); tB.Replace(sc4, sc5); tB.Replace(sc5, sc6); tB.Replace(sc6, sc7); tB.Replace(sc7, sc8); tB.Replace(sc8, sc9); tB.Replace(sc9, sc1);
    //    tC.Replace(sc1, sc2); tC.Replace(sc2, sc3); tC.Replace(sc3, sc4); tC.Replace(sc4, sc5); tC.Replace(sc5, sc6); tC.Replace(sc6, sc7); tC.Replace(sc7, sc8); tC.Replace(sc8, sc9); tC.Replace(sc9, sc1);
    //}

    private static string s_e1 = "a";
    private static string s_e2 = "  ";
    private static string s_e3 = "TeSt!";
    private static string s_e4 = "I think Turkish i \u0131s TROUBL\u0130NG";
    private static string s_e5 = "dzsdzsDDZSDZSDZSddsz";
    private static string s_e6 = "a\u0300\u00C0A\u0300A";
    private static string s_e7 = "Foo\u0400Bar!";
    private static string s_e8 = "a\u0020a\u00A0A\u2000a\u2001a\u2002A\u2003a\u2004a\u2005a";
    private static string s_e9 = "\u4e33\u4e65 Testing... \u4EE8";
    private static string s_e1a = "a";
    private static string s_e2a = "  ";
    private static string s_e3a = "TeSt!";
    private static string s_e4a = "I think Turkish i \u0131s TROUBL\u0130NG";
    private static string s_e5a = "dzsdzsDDZSDZSDZSddsz";
    private static string s_e6a = "a\u0300\u00C0A\u0300A";
    private static string s_e7a = "Foo\u0400Bar!";
    private static string s_e8a = "a\u0020a\u00A0A\u2000a\u2001a\u2002A\u2003a\u2004a\u2005a";
    private static string s_e9a = "\u4e33\u4e65 Testing... \u4EE8";

    [Benchmark]
    public static void Equality()
    {
        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                {
                    bool b;
                    b = s_e1 == s_e2; b = s_e2 == s_e3; b = s_e3 == s_e4; b = s_e4 == s_e5; b = s_e5 == s_e6; b = s_e6 == s_e7; b = s_e7 == s_e8; b = s_e8 == s_e9; b = s_e9 == s_e1;
                    b = s_e1 == s_e1a; b = s_e2 == s_e2a; b = s_e3 == s_e3a; b = s_e4 == s_e4a; b = s_e5 == s_e5a; b = s_e6 == s_e6a; b = s_e7 == s_e7a; b = s_e8 == s_e8a; b = s_e9 == s_e9a;
                }
            }
        }
    }

    [Benchmark]
    public static void RemoveInt()
    {
        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                {
                    s_e1.Remove(0); s_e2.Remove(0); s_e2.Remove(1);
                    s_e3.Remove(0); s_e3.Remove(2); s_e3.Remove(3);
                    s_e4.Remove(0); s_e4.Remove(18); s_e4.Remove(22);
                    s_e5.Remove(0); s_e5.Remove(7); s_e5.Remove(10);
                    s_e6.Remove(0); s_e6.Remove(3); s_e6.Remove(4);
                    s_e7.Remove(0); s_e7.Remove(3); s_e7.Remove(4);
                    s_e8.Remove(0); s_e8.Remove(3);
                    s_e9.Remove(0); s_e9.Remove(4);
                }
            }
        }
    }

    [Benchmark]
    public static void RemoveIntInt()
    {
        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                {
                    s_e1.Remove(0, 0); s_e2.Remove(0, 1); s_e2.Remove(1, 0);
                    s_e3.Remove(0, 2); s_e3.Remove(2, 1); s_e3.Remove(3, 0);
                    s_e4.Remove(0, 3); s_e4.Remove(18, 3); s_e4.Remove(22, 1);
                    s_e5.Remove(0, 8); s_e5.Remove(7, 4); s_e5.Remove(10, 1);
                    s_e6.Remove(0, 2); s_e6.Remove(3, 1); s_e6.Remove(4, 0);
                    s_e7.Remove(0, 4); s_e7.Remove(3, 2); s_e7.Remove(4, 1);
                    s_e8.Remove(0, 2); s_e8.Remove(3, 3);
                    s_e9.Remove(0, 3); s_e9.Remove(4, 1);
                }
            }
        }
    }

    private static string s_f1 = "Testing {0}, {0:C}, {0:D5}, {0:E} - {0:F4}{0:G}{0:N}  {0:X} !!";
    private static string s_f2 = "Testing {0}, {0:C}, {0:E} - {0:F4}{0:G}{0:N} , !!";
    private static string s_f3 = "More testing: {0}";
    private static string s_f4 = "More testing: {0} {1} {2} {3} {4} {5}{6} {7}";

    [Benchmark(InnerIterationCount = 600)]
    public static void Format()
    {
        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                {
                    String.Format(s_f1, 8); String.Format(s_f1, 0); String.Format(s_f2, 0); String.Format(s_f2, -2); String.Format(s_f2, 3.14159); String.Format(s_f2, 11000000);
                    String.Format(s_f3, 0); String.Format(s_f3, -2); String.Format(s_f3, 3.14159); String.Format(s_f3, 11000000); String.Format(s_f3, "Foo");
                    String.Format(s_f3, 'a'); String.Format(s_f3, s_f1);
                    String.Format(s_f4, '1', "Foo", "Foo", "Foo", "Foo", "Foo", "Foo", "Foo");
                }
            }
        }
    }

    private static string s_h1 = String.Empty;
    private static string s_h2 = "  ";
    private static string s_h3 = "TeSt!";
    private static string s_h4 = "I think Turkish i \u0131s TROUBL\u0130NG";
    private static string s_h5 = "dzsdzsDDZSDZSDZSddsz";
    private static string s_h6 = "a\u0300\u00C0A\u0300A";
    private static string s_h7 = "Foo\u0400Bar!";
    private static string s_h8 = "a\u0020a\u00A0A\u2000a\u2001a\u2002A\u2003a\u2004a\u2005a";
    private static string s_h9 = "\u4e33\u4e65 Testing... \u4EE8";

    [Benchmark]
    public static new void GetHashCode()
    {
        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                {
                    s_h1.GetHashCode(); s_h2.GetHashCode(); s_h3.GetHashCode(); s_h4.GetHashCode(); s_h5.GetHashCode(); s_h6.GetHashCode(); s_h7.GetHashCode(); s_h8.GetHashCode(); s_h9.GetHashCode();
                }
            }
        }
    }

    private static string s_p1 = "a";

    [Benchmark]
    public static void PadLeft()
    {
        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                {
                    s_p1.PadLeft(0);
                    s_p1.PadLeft(1);
                    s_p1.PadLeft(5);
                    s_p1.PadLeft(18);
                    s_p1.PadLeft(2142);
                }
            }
        }
    }

    [Benchmark]
    public static void CompareCurrentCulture()
    {
        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                {
                    string.Compare("The quick brown fox", "The quick brown fox");
                    string.Compare("The quick brown fox", "The quick brown fox j");
                    string.Compare("Th\u00e9 quick brown f\u00f2x", "Th\u00e9 quick brown f\u00f2x");
                    string.Compare("Th\u00e9 quick brown f\u00f2x", "Th\u00e9 quick brown f\u00f2x jumped");
                    string.Compare("a\u0300a\u0300a\u0300", "\u00e0\u00e0\u00e0");
                    string.Compare("a\u0300a\u0300a\u0300", "\u00c0\u00c0\u00c0");
                }
            }
        }
    }

    [Benchmark]
    public static void SubstringInt()
    {
        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                {
                    s_e1.Substring(0); s_e2.Substring(0); s_e2.Substring(1);
                    s_e3.Substring(0); s_e3.Substring(2); s_e3.Substring(3);
                    s_e4.Substring(0); s_e4.Substring(18); s_e4.Substring(22);
                    s_e5.Substring(0); s_e5.Substring(7); s_e5.Substring(10);
                    s_e6.Substring(0); s_e6.Substring(3); s_e6.Substring(4);
                    s_e7.Substring(0); s_e7.Substring(3); s_e7.Substring(4);
                    s_e8.Substring(0); s_e8.Substring(3);
                    s_e9.Substring(0); s_e9.Substring(4);
                }
            }
        }
    }

    [Benchmark]
    public static void SubstringIntInt()
    {
        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                {
                    s_e1.Substring(0, 0); s_e2.Substring(0, 1); s_e2.Substring(1, 0);
                    s_e3.Substring(0, 2); s_e3.Substring(2, 1); s_e3.Substring(3, 0);
                    s_e4.Substring(0, 3); s_e4.Substring(18, 3); s_e4.Substring(22, 1);
                    s_e5.Substring(0, 8); s_e5.Substring(7, 4); s_e5.Substring(10, 1);
                    s_e6.Substring(0, 2); s_e6.Substring(3, 1); s_e6.Substring(4, 0);
                    s_e7.Substring(0, 4); s_e7.Substring(3, 2); s_e7.Substring(4, 1);
                    s_e8.Substring(0, 2); s_e8.Substring(3, 3);
                    s_e9.Substring(0, 3); s_e9.Substring(4, 1);
                }
            }
        }
    }

    [Benchmark]
    public static void Split()
    {
        String t1 = String.Empty;
        String t2 = " ";
        String t3 = "   ";
        String t4 = "Test";
        String t5 = " Test";
        String t6 = "Test ";
        String t7 = " Te st  ";
        String t8 = "\u0020\u00A0\u2000\u2001\u2002\u2003\u2004\u2005\t Te\t \tst \t\r\n\u0020\u00A0\u2000\u2001\u2002\u2003\u2004\u2005";
        String t9 = " \u0400Te \u0400st";
        String tA = " a\u0300\u00C0";
        String tB = " a \u0300 \u00C0 ";
        String tC = "     ddsz dszdsz \t  dszdsz  \t        ";

        char[] c1 = null;
        char[] c2 = { 'T' };
        char[] c3 = { 'T', 'T', 'T', 'T', 'T' };
        char[] c4 = { 'a' };
        char[] c5 = { 'T', (char)192 };
        char[] c6 = { ' ', (char)8197 };
        char[] c7 = { "\u0400"[0] };
        char[] c8 = { '1', 'a', ' ', '0', 'd', 'e', 's', 't', "\u0400"[0] };

        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                {
                    t1.Split(c1, StringSplitOptions.None); t2.Split(c1, StringSplitOptions.None); t3.Split(c1, StringSplitOptions.None); t4.Split(c1, StringSplitOptions.None); t5.Split(c1, StringSplitOptions.None); t6.Split(c1, StringSplitOptions.None); t7.Split(c1, StringSplitOptions.None); t8.Split(c1, StringSplitOptions.None); t9.Split(c1, StringSplitOptions.None); tA.Split(c1, StringSplitOptions.None); tB.Split(c1, StringSplitOptions.None); tC.Split(c1, StringSplitOptions.None);
                    t1.Split(c2, StringSplitOptions.None); t2.Split(c2, StringSplitOptions.None); t3.Split(c2, StringSplitOptions.None); t4.Split(c2, StringSplitOptions.None); t5.Split(c2, StringSplitOptions.None); t6.Split(c2, StringSplitOptions.None); t7.Split(c2, StringSplitOptions.None); t8.Split(c2, StringSplitOptions.None); t9.Split(c2, StringSplitOptions.None); tA.Split(c2, StringSplitOptions.None); tB.Split(c2, StringSplitOptions.None); tC.Split(c2, StringSplitOptions.None);
                    t1.Split(c3, StringSplitOptions.None); t2.Split(c3, StringSplitOptions.None); t3.Split(c3, StringSplitOptions.None); t4.Split(c3, StringSplitOptions.None); t5.Split(c3, StringSplitOptions.None); t6.Split(c3, StringSplitOptions.None); t7.Split(c3, StringSplitOptions.None); t8.Split(c3, StringSplitOptions.None); t9.Split(c3, StringSplitOptions.None); tA.Split(c3, StringSplitOptions.None); tB.Split(c3, StringSplitOptions.None); tC.Split(c3, StringSplitOptions.None);
                    t1.Split(c4, StringSplitOptions.None); t2.Split(c4, StringSplitOptions.None); t3.Split(c4, StringSplitOptions.None); t4.Split(c4, StringSplitOptions.None); t5.Split(c4, StringSplitOptions.None); t6.Split(c4, StringSplitOptions.None); t7.Split(c4, StringSplitOptions.None); t8.Split(c4, StringSplitOptions.None); t9.Split(c4, StringSplitOptions.None); tA.Split(c4, StringSplitOptions.None); tB.Split(c4, StringSplitOptions.None); tC.Split(c4, StringSplitOptions.None);
                    t1.Split(c5, StringSplitOptions.None); t2.Split(c5, StringSplitOptions.None); t3.Split(c5, StringSplitOptions.None); t4.Split(c5, StringSplitOptions.None); t5.Split(c5, StringSplitOptions.None); t6.Split(c5, StringSplitOptions.None); t7.Split(c5, StringSplitOptions.None); t8.Split(c5, StringSplitOptions.None); t9.Split(c5, StringSplitOptions.None); tA.Split(c5, StringSplitOptions.None); tB.Split(c5, StringSplitOptions.None); tC.Split(c5, StringSplitOptions.None);
                    t1.Split(c6, StringSplitOptions.None); t2.Split(c6, StringSplitOptions.None); t3.Split(c6, StringSplitOptions.None); t4.Split(c6, StringSplitOptions.None); t5.Split(c6, StringSplitOptions.None); t6.Split(c6, StringSplitOptions.None); t7.Split(c6, StringSplitOptions.None); t8.Split(c6, StringSplitOptions.None); t9.Split(c6, StringSplitOptions.None); tA.Split(c6, StringSplitOptions.None); tB.Split(c6, StringSplitOptions.None); tC.Split(c6, StringSplitOptions.None);
                    t1.Split(c7, StringSplitOptions.None); t2.Split(c7, StringSplitOptions.None); t3.Split(c7, StringSplitOptions.None); t4.Split(c7, StringSplitOptions.None); t5.Split(c7, StringSplitOptions.None); t6.Split(c7, StringSplitOptions.None); t7.Split(c7, StringSplitOptions.None); t8.Split(c7, StringSplitOptions.None); t9.Split(c7, StringSplitOptions.None); tA.Split(c7, StringSplitOptions.None); tB.Split(c7, StringSplitOptions.None); tC.Split(c7, StringSplitOptions.None);
                    t1.Split(c8, StringSplitOptions.None); t2.Split(c8, StringSplitOptions.None); t3.Split(c8, StringSplitOptions.None); t4.Split(c8, StringSplitOptions.None); t5.Split(c8, StringSplitOptions.None); t6.Split(c8, StringSplitOptions.None); t7.Split(c8, StringSplitOptions.None); t8.Split(c8, StringSplitOptions.None); t9.Split(c8, StringSplitOptions.None); tA.Split(c8, StringSplitOptions.None); tB.Split(c8, StringSplitOptions.None); tC.Split(c8, StringSplitOptions.None);

                    t1.Split(c1, StringSplitOptions.RemoveEmptyEntries); t2.Split(c1, StringSplitOptions.RemoveEmptyEntries); t3.Split(c1, StringSplitOptions.RemoveEmptyEntries); t4.Split(c1, StringSplitOptions.RemoveEmptyEntries); t5.Split(c1, StringSplitOptions.RemoveEmptyEntries); t6.Split(c1, StringSplitOptions.RemoveEmptyEntries); t7.Split(c1, StringSplitOptions.RemoveEmptyEntries); t8.Split(c1, StringSplitOptions.RemoveEmptyEntries); t9.Split(c1, StringSplitOptions.RemoveEmptyEntries); tA.Split(c1, StringSplitOptions.RemoveEmptyEntries); tB.Split(c1, StringSplitOptions.RemoveEmptyEntries); tC.Split(c1, StringSplitOptions.RemoveEmptyEntries);
                    t1.Split(c2, StringSplitOptions.RemoveEmptyEntries); t2.Split(c2, StringSplitOptions.RemoveEmptyEntries); t3.Split(c2, StringSplitOptions.RemoveEmptyEntries); t4.Split(c2, StringSplitOptions.RemoveEmptyEntries); t5.Split(c2, StringSplitOptions.RemoveEmptyEntries); t6.Split(c2, StringSplitOptions.RemoveEmptyEntries); t7.Split(c2, StringSplitOptions.RemoveEmptyEntries); t8.Split(c2, StringSplitOptions.RemoveEmptyEntries); t9.Split(c2, StringSplitOptions.RemoveEmptyEntries); tA.Split(c2, StringSplitOptions.RemoveEmptyEntries); tB.Split(c2, StringSplitOptions.RemoveEmptyEntries); tC.Split(c2, StringSplitOptions.RemoveEmptyEntries);
                    t1.Split(c3, StringSplitOptions.RemoveEmptyEntries); t2.Split(c3, StringSplitOptions.RemoveEmptyEntries); t3.Split(c3, StringSplitOptions.RemoveEmptyEntries); t4.Split(c3, StringSplitOptions.RemoveEmptyEntries); t5.Split(c3, StringSplitOptions.RemoveEmptyEntries); t6.Split(c3, StringSplitOptions.RemoveEmptyEntries); t7.Split(c3, StringSplitOptions.RemoveEmptyEntries); t8.Split(c3, StringSplitOptions.RemoveEmptyEntries); t9.Split(c3, StringSplitOptions.RemoveEmptyEntries); tA.Split(c3, StringSplitOptions.RemoveEmptyEntries); tB.Split(c3, StringSplitOptions.RemoveEmptyEntries); tC.Split(c3, StringSplitOptions.RemoveEmptyEntries);
                    t1.Split(c4, StringSplitOptions.RemoveEmptyEntries); t2.Split(c4, StringSplitOptions.RemoveEmptyEntries); t3.Split(c4, StringSplitOptions.RemoveEmptyEntries); t4.Split(c4, StringSplitOptions.RemoveEmptyEntries); t5.Split(c4, StringSplitOptions.RemoveEmptyEntries); t6.Split(c4, StringSplitOptions.RemoveEmptyEntries); t7.Split(c4, StringSplitOptions.RemoveEmptyEntries); t8.Split(c4, StringSplitOptions.RemoveEmptyEntries); t9.Split(c4, StringSplitOptions.RemoveEmptyEntries); tA.Split(c4, StringSplitOptions.RemoveEmptyEntries); tB.Split(c4, StringSplitOptions.RemoveEmptyEntries); tC.Split(c4, StringSplitOptions.RemoveEmptyEntries);
                    t1.Split(c5, StringSplitOptions.RemoveEmptyEntries); t2.Split(c5, StringSplitOptions.RemoveEmptyEntries); t3.Split(c5, StringSplitOptions.RemoveEmptyEntries); t4.Split(c5, StringSplitOptions.RemoveEmptyEntries); t5.Split(c5, StringSplitOptions.RemoveEmptyEntries); t6.Split(c5, StringSplitOptions.RemoveEmptyEntries); t7.Split(c5, StringSplitOptions.RemoveEmptyEntries); t8.Split(c5, StringSplitOptions.RemoveEmptyEntries); t9.Split(c5, StringSplitOptions.RemoveEmptyEntries); tA.Split(c5, StringSplitOptions.RemoveEmptyEntries); tB.Split(c5, StringSplitOptions.RemoveEmptyEntries); tC.Split(c5, StringSplitOptions.RemoveEmptyEntries);
                    t1.Split(c6, StringSplitOptions.RemoveEmptyEntries); t2.Split(c6, StringSplitOptions.RemoveEmptyEntries); t3.Split(c6, StringSplitOptions.RemoveEmptyEntries); t4.Split(c6, StringSplitOptions.RemoveEmptyEntries); t5.Split(c6, StringSplitOptions.RemoveEmptyEntries); t6.Split(c6, StringSplitOptions.RemoveEmptyEntries); t7.Split(c6, StringSplitOptions.RemoveEmptyEntries); t8.Split(c6, StringSplitOptions.RemoveEmptyEntries); t9.Split(c6, StringSplitOptions.RemoveEmptyEntries); tA.Split(c6, StringSplitOptions.RemoveEmptyEntries); tB.Split(c6, StringSplitOptions.RemoveEmptyEntries); tC.Split(c6, StringSplitOptions.RemoveEmptyEntries);
                    t1.Split(c7, StringSplitOptions.RemoveEmptyEntries); t2.Split(c7, StringSplitOptions.RemoveEmptyEntries); t3.Split(c7, StringSplitOptions.RemoveEmptyEntries); t4.Split(c7, StringSplitOptions.RemoveEmptyEntries); t5.Split(c7, StringSplitOptions.RemoveEmptyEntries); t6.Split(c7, StringSplitOptions.RemoveEmptyEntries); t7.Split(c7, StringSplitOptions.RemoveEmptyEntries); t8.Split(c7, StringSplitOptions.RemoveEmptyEntries); t9.Split(c7, StringSplitOptions.RemoveEmptyEntries); tA.Split(c7, StringSplitOptions.RemoveEmptyEntries); tB.Split(c7, StringSplitOptions.RemoveEmptyEntries); tC.Split(c7, StringSplitOptions.RemoveEmptyEntries);
                    t1.Split(c8, StringSplitOptions.RemoveEmptyEntries); t2.Split(c8, StringSplitOptions.RemoveEmptyEntries); t3.Split(c8, StringSplitOptions.RemoveEmptyEntries); t4.Split(c8, StringSplitOptions.RemoveEmptyEntries); t5.Split(c8, StringSplitOptions.RemoveEmptyEntries); t6.Split(c8, StringSplitOptions.RemoveEmptyEntries); t7.Split(c8, StringSplitOptions.RemoveEmptyEntries); t8.Split(c8, StringSplitOptions.RemoveEmptyEntries); t9.Split(c8, StringSplitOptions.RemoveEmptyEntries); tA.Split(c8, StringSplitOptions.RemoveEmptyEntries); tB.Split(c8, StringSplitOptions.RemoveEmptyEntries); tC.Split(c8, StringSplitOptions.RemoveEmptyEntries);
                }
            }
        }
    }
    #endregion
}