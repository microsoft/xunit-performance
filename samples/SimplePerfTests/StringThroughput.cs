using Microsoft.Xunit.Performance;
using System;
using System.Runtime.CompilerServices;

public static class StringThroughput
{
    static string s1 = String.Empty;
    static string s2 = " ";
    static string s3 = "TeSt";
    static string s4 = "TEST";
    static string s5 = "test";
    static string s6 = "I think Turkish i \u0131s TROUBL\u0130NG";
    static string s7 = "dzsdzsDDZSDZSDZSddsz";
    static string s8 = "a\u0300\u00C0A\u0300";
    static string s9 = "Foo\u0400Bar";
    static string sA = "a\u0020a\u00A0A\u2000a\u2001a\u2002A\u2003a\u2004a\u2005a";
    static string sB = "\u4e33\u4e65 Testing... \u4EE8";

    static char[] ca1 = null;
    static char[] ca2 = { 'T' };
    static char[] ca3 = { 'T', 'T', 'T', 'T', 'T' };
    static char[] ca4 = { 'a' };
    static char[] ca5 = { 'T', (char)192 };
    static char[] ca6 = { ' ', (char)8197 };
    static char[] ca7 = { "\u0400"[0] };
    static char[] ca8 = { '1', 'a', ' ', '0', 'd', 'e', 's', 't', "\u0400"[0] };


    [Benchmark]
    public static void ToUpper()
    {
        s1.ToUpper(); s2.ToUpper(); s3.ToUpper(); s4.ToUpper(); s5.ToUpper(); s6.ToUpper(); s7.ToUpper(); s8.ToUpper(); s9.ToUpper(); sA.ToUpper(); sB.ToUpper();
    }

    [Benchmark]
    public static void ToUpperInvariant()
    {
        s1.ToUpperInvariant(); s2.ToUpperInvariant(); s3.ToUpperInvariant(); s4.ToUpperInvariant(); s5.ToUpperInvariant(); s6.ToUpperInvariant(); s7.ToUpperInvariant(); s8.ToUpperInvariant(); s9.ToUpperInvariant(); sA.ToUpperInvariant(); sB.ToUpperInvariant();
    }

    static string t1 = String.Empty;
    static string t2 = " ";
    static string t3 = "   ";
    static string t4 = "Test";
    static string t5 = " Test";
    static string t6 = "Test ";
    static string t7 = " Te st  ";
    static string t8 = "\u0020\u00A0\u2000\u2001\u2002\u2003\u2004\u2005\t Te\t \tst \t\r\n\u0020\u00A0\u2000\u2001\u2002\u2003\u2004\u2005";
    static string t9 = " \u0400Te \u0400st";
    static string tA = " a\u0300\u00C0";
    static string tB = " a \u0300 \u00C0 ";
    static string tC = "     ddsz dszdsz \t  dszdsz  \t        ";

    [Benchmark]
    public static void Trim()
    {
        t1.Trim(); t2.Trim(); t3.Trim(); t4.Trim(); t5.Trim(); t6.Trim(); t7.Trim(); t8.Trim(); t9.Trim(); tA.Trim(); tB.Trim(); tC.Trim();
    }

    [Benchmark]
    public static void TrimCharArr()
    {
        t1.Trim(ca1); t2.Trim(ca1); t3.Trim(ca1); t4.Trim(ca1); t5.Trim(ca1); t6.Trim(ca1); t7.Trim(ca1); t8.Trim(ca1); t9.Trim(ca1); tA.Trim(ca1); tB.Trim(ca1); tC.Trim(ca1);
        t1.Trim(ca2); t2.Trim(ca2); t3.Trim(ca2); t4.Trim(ca2); t5.Trim(ca2); t6.Trim(ca2); t7.Trim(ca2); t8.Trim(ca2); t9.Trim(ca2); tA.Trim(ca2); tB.Trim(ca2); tC.Trim(ca2);
        t1.Trim(ca3); t2.Trim(ca3); t3.Trim(ca3); t4.Trim(ca3); t5.Trim(ca3); t6.Trim(ca3); t7.Trim(ca3); t8.Trim(ca3); t9.Trim(ca3); tA.Trim(ca3); tB.Trim(ca3); tC.Trim(ca3);
        t1.Trim(ca4); t2.Trim(ca4); t3.Trim(ca4); t4.Trim(ca4); t5.Trim(ca4); t6.Trim(ca4); t7.Trim(ca4); t8.Trim(ca4); t9.Trim(ca4); tA.Trim(ca4); tB.Trim(ca4); tC.Trim(ca4);
        t1.Trim(ca5); t2.Trim(ca5); t3.Trim(ca5); t4.Trim(ca5); t5.Trim(ca5); t6.Trim(ca5); t7.Trim(ca5); t8.Trim(ca5); t9.Trim(ca5); tA.Trim(ca5); tB.Trim(ca5); tC.Trim(ca5);
        t1.Trim(ca6); t2.Trim(ca6); t3.Trim(ca6); t4.Trim(ca6); t5.Trim(ca6); t6.Trim(ca6); t7.Trim(ca6); t8.Trim(ca6); t9.Trim(ca6); tA.Trim(ca6); tB.Trim(ca6); tC.Trim(ca6);
        t1.Trim(ca7); t2.Trim(ca7); t3.Trim(ca7); t4.Trim(ca7); t5.Trim(ca7); t6.Trim(ca7); t7.Trim(ca7); t8.Trim(ca7); t9.Trim(ca7); tA.Trim(ca7); tB.Trim(ca7); tC.Trim(ca7);
        t1.Trim(ca8); t2.Trim(ca8); t3.Trim(ca8); t4.Trim(ca8); t5.Trim(ca8); t6.Trim(ca8); t7.Trim(ca8); t8.Trim(ca8); t9.Trim(ca8); tA.Trim(ca8); tB.Trim(ca8); tC.Trim(ca8);
    }

    [Benchmark]
    public static void TrimStart()
    {
        t1.TrimStart(); t2.TrimStart(); t3.TrimStart(); t4.TrimStart(); t5.TrimStart(); t6.TrimStart(); t7.TrimStart(); t8.TrimStart(); t9.TrimStart(); tA.TrimStart(); tB.TrimStart(); tC.TrimStart();
        t1.TrimStart(ca1); t2.TrimStart(ca1); t3.TrimStart(ca1); t4.TrimStart(ca1); t5.TrimStart(ca1); t6.TrimStart(ca1); t7.TrimStart(ca1); t8.TrimStart(ca1); t9.TrimStart(ca1); tA.TrimStart(ca1); tB.TrimStart(ca1); tC.TrimStart(ca1);
        t1.TrimStart(ca2); t2.TrimStart(ca2); t3.TrimStart(ca2); t4.TrimStart(ca2); t5.TrimStart(ca2); t6.TrimStart(ca2); t7.TrimStart(ca2); t8.TrimStart(ca2); t9.TrimStart(ca2); tA.TrimStart(ca2); tB.TrimStart(ca2); tC.TrimStart(ca2);
        t1.TrimStart(ca3); t2.TrimStart(ca3); t3.TrimStart(ca3); t4.TrimStart(ca3); t5.TrimStart(ca3); t6.TrimStart(ca3); t7.TrimStart(ca3); t8.TrimStart(ca3); t9.TrimStart(ca3); tA.TrimStart(ca3); tB.TrimStart(ca3); tC.TrimStart(ca3);
        t1.TrimStart(ca4); t2.TrimStart(ca4); t3.TrimStart(ca4); t4.TrimStart(ca4); t5.TrimStart(ca4); t6.TrimStart(ca4); t7.TrimStart(ca4); t8.TrimStart(ca4); t9.TrimStart(ca4); tA.TrimStart(ca4); tB.TrimStart(ca4); tC.TrimStart(ca4);
        t1.TrimStart(ca5); t2.TrimStart(ca5); t3.TrimStart(ca5); t4.TrimStart(ca5); t5.TrimStart(ca5); t6.TrimStart(ca5); t7.TrimStart(ca5); t8.TrimStart(ca5); t9.TrimStart(ca5); tA.TrimStart(ca5); tB.TrimStart(ca5); tC.TrimStart(ca5);
        t1.TrimStart(ca6); t2.TrimStart(ca6); t3.TrimStart(ca6); t4.TrimStart(ca6); t5.TrimStart(ca6); t6.TrimStart(ca6); t7.TrimStart(ca6); t8.TrimStart(ca6); t9.TrimStart(ca6); tA.TrimStart(ca6); tB.TrimStart(ca6); tC.TrimStart(ca6);
        t1.TrimStart(ca7); t2.TrimStart(ca7); t3.TrimStart(ca7); t4.TrimStart(ca7); t5.TrimStart(ca7); t6.TrimStart(ca7); t7.TrimStart(ca7); t8.TrimStart(ca7); t9.TrimStart(ca7); tA.TrimStart(ca7); tB.TrimStart(ca7); tC.TrimStart(ca7);
        t1.TrimStart(ca8); t2.TrimStart(ca8); t3.TrimStart(ca8); t4.TrimStart(ca8); t5.TrimStart(ca8); t6.TrimStart(ca8); t7.TrimStart(ca8); t8.TrimStart(ca8); t9.TrimStart(ca8); tA.TrimStart(ca8); tB.TrimStart(ca8); tC.TrimStart(ca8);
    }

    [Benchmark]
    public static void TrimEnd()
    {
        t1.TrimEnd(); t2.TrimEnd(); t3.TrimEnd(); t4.TrimEnd(); t5.TrimEnd(); t6.TrimEnd(); t7.TrimEnd(); t8.TrimEnd(); t9.TrimEnd(); tA.TrimEnd(); tB.TrimEnd(); tC.TrimEnd();
        t1.TrimEnd(ca1); t2.TrimEnd(ca1); t3.TrimEnd(ca1); t4.TrimEnd(ca1); t5.TrimEnd(ca1); t6.TrimEnd(ca1); t7.TrimEnd(ca1); t8.TrimEnd(ca1); t9.TrimEnd(ca1); tA.TrimEnd(ca1); tB.TrimEnd(ca1); tC.TrimEnd(ca1);
        t1.TrimEnd(ca2); t2.TrimEnd(ca2); t3.TrimEnd(ca2); t4.TrimEnd(ca2); t5.TrimEnd(ca2); t6.TrimEnd(ca2); t7.TrimEnd(ca2); t8.TrimEnd(ca2); t9.TrimEnd(ca2); tA.TrimEnd(ca2); tB.TrimEnd(ca2); tC.TrimEnd(ca2);
        t1.TrimEnd(ca3); t2.TrimEnd(ca3); t3.TrimEnd(ca3); t4.TrimEnd(ca3); t5.TrimEnd(ca3); t6.TrimEnd(ca3); t7.TrimEnd(ca3); t8.TrimEnd(ca3); t9.TrimEnd(ca3); tA.TrimEnd(ca3); tB.TrimEnd(ca3); tC.TrimEnd(ca3);
        t1.TrimEnd(ca4); t2.TrimEnd(ca4); t3.TrimEnd(ca4); t4.TrimEnd(ca4); t5.TrimEnd(ca4); t6.TrimEnd(ca4); t7.TrimEnd(ca4); t8.TrimEnd(ca4); t9.TrimEnd(ca4); tA.TrimEnd(ca4); tB.TrimEnd(ca4); tC.TrimEnd(ca4);
        t1.TrimEnd(ca5); t2.TrimEnd(ca5); t3.TrimEnd(ca5); t4.TrimEnd(ca5); t5.TrimEnd(ca5); t6.TrimEnd(ca5); t7.TrimEnd(ca5); t8.TrimEnd(ca5); t9.TrimEnd(ca5); tA.TrimEnd(ca5); tB.TrimEnd(ca5); tC.TrimEnd(ca5);
        t1.TrimEnd(ca6); t2.TrimEnd(ca6); t3.TrimEnd(ca6); t4.TrimEnd(ca6); t5.TrimEnd(ca6); t6.TrimEnd(ca6); t7.TrimEnd(ca6); t8.TrimEnd(ca6); t9.TrimEnd(ca6); tA.TrimEnd(ca6); tB.TrimEnd(ca6); tC.TrimEnd(ca6);
        t1.TrimEnd(ca7); t2.TrimEnd(ca7); t3.TrimEnd(ca7); t4.TrimEnd(ca7); t5.TrimEnd(ca7); t6.TrimEnd(ca7); t7.TrimEnd(ca7); t8.TrimEnd(ca7); t9.TrimEnd(ca7); tA.TrimEnd(ca7); tB.TrimEnd(ca7); tC.TrimEnd(ca7);
        t1.TrimEnd(ca8); t2.TrimEnd(ca8); t3.TrimEnd(ca8); t4.TrimEnd(ca8); t5.TrimEnd(ca8); t6.TrimEnd(ca8); t7.TrimEnd(ca8); t8.TrimEnd(ca8); t9.TrimEnd(ca8); tA.TrimEnd(ca8); tB.TrimEnd(ca8); tC.TrimEnd(ca8);
    }

    [Benchmark]
    public static void Insert()
    {
        t1.Insert(0, t2); t2.Insert(1, t3); t3.Insert(1, t4); t4.Insert(2, t5); t5.Insert(2, t6); t6.Insert(2, t7);
        t7.Insert(3, t8); t8.Insert(3, t9); t9.Insert(3, tA); tA.Insert(3, tB); tB.Insert(4, tC); tC.Insert(5, t1);
    }

    static char c1 = ' ';
    static char c2 = 'T';
    static char c3 = 'd';
    static char c4 = 'a';
    static char c5 = (char)192;
    static char c6 = (char)8197;
    static char c7 = "\u0400"[0];
    static char c8 = '\t';
    static char c9 = (char)768;

    [Benchmark]
    public static void ReplaceChar()
    {
        t1.Replace(c1, c2); t1.Replace(c2, c3); t1.Replace(c3, c4); t1.Replace(c4, c5); t1.Replace(c5, c6); t1.Replace(c6, c7); t1.Replace(c7, c8); t1.Replace(c8, c9); t1.Replace(c9, c1);
        t2.Replace(c1, c2); t2.Replace(c2, c3); t2.Replace(c3, c4); t2.Replace(c4, c5); t2.Replace(c5, c6); t2.Replace(c6, c7); t2.Replace(c7, c8); t2.Replace(c8, c9); t2.Replace(c9, c1);
        t3.Replace(c1, c2); t3.Replace(c2, c3); t3.Replace(c3, c4); t3.Replace(c4, c5); t3.Replace(c5, c6); t3.Replace(c6, c7); t3.Replace(c7, c8); t3.Replace(c8, c9); t3.Replace(c9, c1);
        t4.Replace(c1, c2); t4.Replace(c2, c3); t4.Replace(c3, c4); t4.Replace(c4, c5); t4.Replace(c5, c6); t4.Replace(c6, c7); t4.Replace(c7, c8); t4.Replace(c8, c9); t4.Replace(c9, c1);
        t5.Replace(c1, c2); t5.Replace(c2, c3); t5.Replace(c3, c4); t5.Replace(c4, c5); t5.Replace(c5, c6); t5.Replace(c6, c7); t5.Replace(c7, c8); t5.Replace(c8, c9); t5.Replace(c9, c1);
        t6.Replace(c1, c2); t6.Replace(c2, c3); t6.Replace(c3, c4); t6.Replace(c4, c5); t6.Replace(c5, c6); t6.Replace(c6, c7); t6.Replace(c7, c8); t6.Replace(c8, c9); t6.Replace(c9, c1);
        t7.Replace(c1, c2); t7.Replace(c2, c3); t7.Replace(c3, c4); t7.Replace(c4, c5); t7.Replace(c5, c6); t7.Replace(c6, c7); t7.Replace(c7, c8); t7.Replace(c8, c9); t7.Replace(c9, c1);
        t8.Replace(c1, c2); t8.Replace(c2, c3); t8.Replace(c3, c4); t8.Replace(c4, c5); t8.Replace(c5, c6); t8.Replace(c6, c7); t8.Replace(c7, c8); t8.Replace(c8, c9); t8.Replace(c9, c1);
        t9.Replace(c1, c2); t9.Replace(c2, c3); t9.Replace(c3, c4); t9.Replace(c4, c5); t9.Replace(c5, c6); t9.Replace(c6, c7); t9.Replace(c7, c8); t9.Replace(c8, c9); t9.Replace(c9, c1);
        tA.Replace(c1, c2); tA.Replace(c2, c3); tA.Replace(c3, c4); tA.Replace(c4, c5); tA.Replace(c5, c6); tA.Replace(c6, c7); tA.Replace(c7, c8); tA.Replace(c8, c9); tA.Replace(c9, c1);
        tB.Replace(c1, c2); tB.Replace(c2, c3); tB.Replace(c3, c4); tB.Replace(c4, c5); tB.Replace(c5, c6); tB.Replace(c6, c7); tB.Replace(c7, c8); tB.Replace(c8, c9); tB.Replace(c9, c1);
        tC.Replace(c1, c2); tC.Replace(c2, c3); tC.Replace(c3, c4); tC.Replace(c4, c5); tC.Replace(c5, c6); tC.Replace(c6, c7); tC.Replace(c7, c8); tC.Replace(c8, c9); tC.Replace(c9, c1);
    }

    static string sc1 = "  ";
    static string sc2 = "T";
    static string sc3 = "dd";
    static string sc4 = "a";
    static string sc5 = "\u00C0";
    static string sc6 = "a\u0300";
    static string sc7 = "\u0400";
    static string sc8 = "\u0300";
    static string sc9 = "\u00A0\u2000";

    [Benchmark]
    public static void ReplaceString()
    {
        t1.Replace(sc1, sc2); t1.Replace(sc2, sc3); t1.Replace(sc3, sc4); t1.Replace(sc4, sc5); t1.Replace(sc5, sc6); t1.Replace(sc6, sc7); t1.Replace(sc7, sc8); t1.Replace(sc8, sc9); t1.Replace(sc9, sc1);
        t2.Replace(sc1, sc2); t2.Replace(sc2, sc3); t2.Replace(sc3, sc4); t2.Replace(sc4, sc5); t2.Replace(sc5, sc6); t2.Replace(sc6, sc7); t2.Replace(sc7, sc8); t2.Replace(sc8, sc9); t2.Replace(sc9, sc1);
        t3.Replace(sc1, sc2); t3.Replace(sc2, sc3); t3.Replace(sc3, sc4); t3.Replace(sc4, sc5); t3.Replace(sc5, sc6); t3.Replace(sc6, sc7); t3.Replace(sc7, sc8); t3.Replace(sc8, sc9); t3.Replace(sc9, sc1);
        t4.Replace(sc1, sc2); t4.Replace(sc2, sc3); t4.Replace(sc3, sc4); t4.Replace(sc4, sc5); t4.Replace(sc5, sc6); t4.Replace(sc6, sc7); t4.Replace(sc7, sc8); t4.Replace(sc8, sc9); t4.Replace(sc9, sc1);
        t5.Replace(sc1, sc2); t5.Replace(sc2, sc3); t5.Replace(sc3, sc4); t5.Replace(sc4, sc5); t5.Replace(sc5, sc6); t5.Replace(sc6, sc7); t5.Replace(sc7, sc8); t5.Replace(sc8, sc9); t5.Replace(sc9, sc1);
        t6.Replace(sc1, sc2); t6.Replace(sc2, sc3); t6.Replace(sc3, sc4); t6.Replace(sc4, sc5); t6.Replace(sc5, sc6); t6.Replace(sc6, sc7); t6.Replace(sc7, sc8); t6.Replace(sc8, sc9); t6.Replace(sc9, sc1);
        t7.Replace(sc1, sc2); t7.Replace(sc2, sc3); t7.Replace(sc3, sc4); t7.Replace(sc4, sc5); t7.Replace(sc5, sc6); t7.Replace(sc6, sc7); t7.Replace(sc7, sc8); t7.Replace(sc8, sc9); t7.Replace(sc9, sc1);
        t8.Replace(sc1, sc2); t8.Replace(sc2, sc3); t8.Replace(sc3, sc4); t8.Replace(sc4, sc5); t8.Replace(sc5, sc6); t8.Replace(sc6, sc7); t8.Replace(sc7, sc8); t8.Replace(sc8, sc9); t8.Replace(sc9, sc1);
        t9.Replace(sc1, sc2); t9.Replace(sc2, sc3); t9.Replace(sc3, sc4); t9.Replace(sc4, sc5); t9.Replace(sc5, sc6); t9.Replace(sc6, sc7); t9.Replace(sc7, sc8); t9.Replace(sc8, sc9); t9.Replace(sc9, sc1);
        tA.Replace(sc1, sc2); tA.Replace(sc2, sc3); tA.Replace(sc3, sc4); tA.Replace(sc4, sc5); tA.Replace(sc5, sc6); tA.Replace(sc6, sc7); tA.Replace(sc7, sc8); tA.Replace(sc8, sc9); tA.Replace(sc9, sc1);
        tB.Replace(sc1, sc2); tB.Replace(sc2, sc3); tB.Replace(sc3, sc4); tB.Replace(sc4, sc5); tB.Replace(sc5, sc6); tB.Replace(sc6, sc7); tB.Replace(sc7, sc8); tB.Replace(sc8, sc9); tB.Replace(sc9, sc1);
        tC.Replace(sc1, sc2); tC.Replace(sc2, sc3); tC.Replace(sc3, sc4); tC.Replace(sc4, sc5); tC.Replace(sc5, sc6); tC.Replace(sc6, sc7); tC.Replace(sc7, sc8); tC.Replace(sc8, sc9); tC.Replace(sc9, sc1);
    }

    static string e1 = "a";
    static string e2 = "  ";
    static string e3 = "TeSt!";
    static string e4 = "I think Turkish i \u0131s TROUBL\u0130NG";
    static string e5 = "dzsdzsDDZSDZSDZSddsz";
    static string e6 = "a\u0300\u00C0A\u0300A";
    static string e7 = "Foo\u0400Bar!";
    static string e8 = "a\u0020a\u00A0A\u2000a\u2001a\u2002A\u2003a\u2004a\u2005a";
    static string e9 = "\u4e33\u4e65 Testing... \u4EE8";
    static string e1a = "a";
    static string e2a = "  ";
    static string e3a = "TeSt!";
    static string e4a = "I think Turkish i \u0131s TROUBL\u0130NG";
    static string e5a = "dzsdzsDDZSDZSDZSddsz";
    static string e6a = "a\u0300\u00C0A\u0300A";
    static string e7a = "Foo\u0400Bar!";
    static string e8a = "a\u0020a\u00A0A\u2000a\u2001a\u2002A\u2003a\u2004a\u2005a";
    static string e9a = "\u4e33\u4e65 Testing... \u4EE8";

    [Benchmark]
    public static void Equality()
    {
        bool b;
        b = e1 == e2; b = e2 == e3; b = e3 == e4; b = e4 == e5; b = e5 == e6; b = e6 == e7; b = e7 == e8; b = e8 == e9; b = e9 == e1;
        b = e1 == e1a; b = e2 == e2a; b = e3 == e3a; b = e4 == e4a; b = e5 == e5a; b = e6 == e6a; b = e7 == e7a; b = e8 == e8a; b = e9 == e9a;
    }

    [Benchmark]
    public static void RemoveInt()
    {
        e1.Remove(0); e2.Remove(0); e2.Remove(1);
        e3.Remove(0); e3.Remove(2); e3.Remove(3);
        e4.Remove(0); e4.Remove(18); e4.Remove(22);
        e5.Remove(0); e5.Remove(7); e5.Remove(10);
        e6.Remove(0); e6.Remove(3); e6.Remove(4);
        e7.Remove(0); e7.Remove(3); e7.Remove(4);
        e8.Remove(0); e8.Remove(3);
        e9.Remove(0); e9.Remove(4);
    }

    [Benchmark]
    public static void RemoveIntInt()
    {
        e1.Remove(0, 0); e2.Remove(0, 1); e2.Remove(1, 0);
        e3.Remove(0, 2); e3.Remove(2, 1); e3.Remove(3, 0);
        e4.Remove(0, 3); e4.Remove(18, 3); e4.Remove(22, 1);
        e5.Remove(0, 8); e5.Remove(7, 4); e5.Remove(10, 1);
        e6.Remove(0, 2); e6.Remove(3, 1); e6.Remove(4, 0);
        e7.Remove(0, 4); e7.Remove(3, 2); e7.Remove(4, 1);
        e8.Remove(0, 2); e8.Remove(3, 3);
        e9.Remove(0, 3); e9.Remove(4, 1);
    }

    static string f1 = "Testing {0}, {0:C}, {0:D5}, {0:E} - {0:F4}{0:G}{0:N}  {0:X} !!";
    static string f2 = "Testing {0}, {0:C}, {0:E} - {0:F4}{0:G}{0:N} , !!";
    static string f3 = "More testing: {0}";
    static string f4 = "More testing: {0} {1} {2} {3} {4} {5}{6} {7}";

    [Benchmark]
    public static void Format()
    {
        String.Format(f1, 8); String.Format(f1, 0); String.Format(f2, 0); String.Format(f2, -2); String.Format(f2, 3.14159); String.Format(f2, 11000000);
        String.Format(f3, 0); String.Format(f3, -2); String.Format(f3, 3.14159); String.Format(f3, 11000000); String.Format(f3, "Foo");
        String.Format(f3, 'a'); String.Format(f3, f1);
        String.Format(f4, '1', "Foo", "Foo", "Foo", "Foo", "Foo", "Foo", "Foo");
    }

    static string h1 = String.Empty;
    static string h2 = "  ";
    static string h3 = "TeSt!";
    static string h4 = "I think Turkish i \u0131s TROUBL\u0130NG";
    static string h5 = "dzsdzsDDZSDZSDZSddsz";
    static string h6 = "a\u0300\u00C0A\u0300A";
    static string h7 = "Foo\u0400Bar!";
    static string h8 = "a\u0020a\u00A0A\u2000a\u2001a\u2002A\u2003a\u2004a\u2005a";
    static string h9 = "\u4e33\u4e65 Testing... \u4EE8";

    [Benchmark]
    public static new void GetHashCode()
    {
        h1.GetHashCode(); h2.GetHashCode(); h3.GetHashCode(); h4.GetHashCode(); h5.GetHashCode(); h6.GetHashCode(); h7.GetHashCode(); h8.GetHashCode(); h9.GetHashCode();
    }

    public static string i1 = "ddsz dszdsz \t  dszdsz  a\u0300\u00C0 \t Te st \u0400Te \u0400st\u0020\u00A0\u2000\u2001\u2002\u2003\u2004\u2005\t Te\t \tst \t\r\n\u0020\u00A0\u2000\u2001\u2002\u2003\u2004\u2005";
    public static int counter;

    [Benchmark]
    public static void BoundCheckHoist()
    {
        int strLength = i1.Length;

        for (int j = 0; j < strLength; j++)
        {
            counter += i1[j];
        }
    }

    [Benchmark]
    public static void LengthHoisting()
    {
        for (int j = 0; j < i1.Length; j++)
        {
            counter += i1[j];
        }
    }

    [Benchmark]
    public static void PathLength()
    {
        for (int j = 0; j < i1.Length; j++)
        {
            counter += GetStringCharNoInline(i1, j);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static char GetStringCharNoInline(string str, int index)
    {
        return str[index];
    }

    static string p1 = "a";

    [Benchmark]
    public static void PadLeft()
    {
        p1.PadLeft(0);
        p1.PadLeft(1);
        p1.PadLeft(5);
        p1.PadLeft(18);
        p1.PadLeft(2142);
    }

    [Benchmark]
    public static void CompareCurrentCulture()
    {
        string.Compare("The quick brown fox", "The quick brown fox");
        string.Compare("The quick brown fox", "The quick brown fox j");
        string.Compare("Th\u00e9 quick brown f\u00f2x", "Th\u00e9 quick brown f\u00f2x");
        string.Compare("Th\u00e9 quick brown f\u00f2x", "Th\u00e9 quick brown f\u00f2x jumped");
        string.Compare("a\u0300a\u0300a\u0300", "\u00e0\u00e0\u00e0");
        string.Compare("a\u0300a\u0300a\u0300", "\u00c0\u00c0\u00c0");
    }

    [Benchmark]
    public static void SubstringInt()
    {
        e1.Substring(0); e2.Substring(0); e2.Substring(1);
        e3.Substring(0); e3.Substring(2); e3.Substring(3);
        e4.Substring(0); e4.Substring(18); e4.Substring(22);
        e5.Substring(0); e5.Substring(7); e5.Substring(10);
        e6.Substring(0); e6.Substring(3); e6.Substring(4);
        e7.Substring(0); e7.Substring(3); e7.Substring(4);
        e8.Substring(0); e8.Substring(3);
        e9.Substring(0); e9.Substring(4);
    }

    [Benchmark]
    public static void SubstringIntInt()
    {
        e1.Substring(0, 0); e2.Substring(0, 1); e2.Substring(1, 0);
        e3.Substring(0, 2); e3.Substring(2, 1); e3.Substring(3, 0);
        e4.Substring(0, 3); e4.Substring(18, 3); e4.Substring(22, 1);
        e5.Substring(0, 8); e5.Substring(7, 4); e5.Substring(10, 1);
        e6.Substring(0, 2); e6.Substring(3, 1); e6.Substring(4, 0);
        e7.Substring(0, 4); e7.Substring(3, 2); e7.Substring(4, 1);
        e8.Substring(0, 2); e8.Substring(3, 3);
        e9.Substring(0, 3); e9.Substring(4, 1);
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