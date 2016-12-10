using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Microsoft.Xunit.Performance.Api
{
    internal static class ArgParse
    {
        public static string[] SplitCommandLine(string commandLine)
        {
            int pNumArgs;
            IntPtr argv = Native.Windows.Shell32.CommandLineToArgvW(commandLine, out pNumArgs);
            if (argv == IntPtr.Zero)
                throw new Win32Exception($"Unable to parse command line string \"{commandLine}\"");

            try
            {
                var args = new string[pNumArgs];
                for (var i = 0; i < pNumArgs; ++i)
                    args[i] = Marshal.PtrToStringUni(Marshal.ReadIntPtr(argv, i * IntPtr.Size));
                return args;
            }
            finally
            {
                Native.Windows.Kernel32.LocalFree(argv);
            }
        }
    }

    namespace Native.Windows
    {
        internal static class Shell32
        {
            [DllImport("shell32.dll", SetLastError = true)]
            public static extern IntPtr CommandLineToArgvW(
                [MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine,
                out int pNumArgs);
        }

        internal static class Kernel32
        {
            [DllImport("kernel32.dll")]
            public static extern IntPtr LocalFree(IntPtr hMem);
        }
    }
}
