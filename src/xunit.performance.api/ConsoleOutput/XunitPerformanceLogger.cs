using System;
using System.Diagnostics;

namespace Microsoft.Xunit.Performance.Api
{
    /// <summary>
    /// This class currently wraps Console.Out.WriteLine, and it is an interface
    /// to log: information, warning, errors and debug messages in a standard
    /// way with timestamp.
    /// </summary>
    internal static class XunitPerformanceLogger
    {
        public static void WriteInfoLine(string value)
        {
            WriteLine($"[INF] {value}", ConsoleColor.Black, ConsoleColor.White);
        }

        public static void WriteErrorLine(string value)
        {
            WriteLine($"[ERR] {value}", ConsoleColor.Black, ConsoleColor.Red);
        }

        public static void WriteWarningLine(string value)
        {
            WriteLine($"[WRN] {value}", ConsoleColor.Black, ConsoleColor.Yellow);
        }

        [Conditional("DEBUG")]
        public static void WriteDebugLine(string value)
        {
            WriteLine($"[DBG] {value}", ConsoleColor.Yellow, ConsoleColor.Blue);
        }

        private static void WriteLine(string value, ConsoleColor background, ConsoleColor foreground)
        {
            using (var conColor = new ConColor(background, foreground))
                Console.Out.WriteLine($"[{DateTime.Now}]{value}");
        }

        private sealed class ConColor : IDisposable
        {
            public ConColor(ConsoleColor background, ConsoleColor foreground)
            {
                // Save previous setting.
                _background = Console.BackgroundColor;
                _foreground = Console.ForegroundColor;

                // Set new setting.
                Console.BackgroundColor = background;
                Console.ForegroundColor = foreground;
            }

            public void Dispose()
            {
                // Restore previous setting.
                Console.BackgroundColor = _background;
                Console.ForegroundColor = _foreground;
            }

            private ConsoleColor _background;
            private ConsoleColor _foreground;
        }
    }
}
