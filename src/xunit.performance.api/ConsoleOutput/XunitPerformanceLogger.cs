using System;

namespace Microsoft.Xunit.Performance.Api
{
    internal static class XunitPerformanceLogger
    {
        public static void WriteInfoLine(string value)
        {
            WriteLine($"[INFO ] {value}", ConsoleColor.Black, ConsoleColor.White);
        }

        public static void WriteErrorLine(string value)
        {
            WriteLine($"[ERROR] {value}", ConsoleColor.Black, ConsoleColor.Red);
        }

        public static void WriteWarningLine(string value)
        {
            WriteLine($"[WARN ] {value}", ConsoleColor.Black, ConsoleColor.Yellow);
        }

        public static void WriteDebugLine(string value)
        {
            WriteLine($"[DEBUG] {value}", ConsoleColor.Yellow, ConsoleColor.Blue);
        }

        private static void WriteLine(string value, ConsoleColor background, ConsoleColor foreground)
        {
            Console.BackgroundColor = background;
            Console.ForegroundColor = foreground;
            Console.Out.WriteLine($"[{DateTime.Now}] {value}");
            Console.ResetColor();
        }
    }
}
