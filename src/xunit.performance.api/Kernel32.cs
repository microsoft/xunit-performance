using System;
using System.Runtime.InteropServices;

namespace Microsoft.Xunit.Performance.Api.Native.Windows
{
    /*
     * OSVersion is only available on netstandard1.7, and this does not support netcoreapp1.0
     * (it would stop our ability to test .NET Core 1.0)
     * The implementation below is based on VersionHelpers.h
     */

    static partial class Kernel32
    {
        public delegate bool PHANDLER_ROUTINE(CtrlTypes dwCtrlType);

        [Flags]
        public enum CtrlTypes : uint
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        [Flags]
        enum ConditionMask : byte
        {
            VER_EQUAL = 1,
            VER_GREATER = 2,
            VER_GREATER_EQUAL = 3,
            VER_LESS = 4,
            VER_LESS_EQUAL = 5
        }

        [Flags]
        enum TypeMask : uint
        {
            VER_BUILDNUMBER = 0x0000004,
            VER_MAJORVERSION = 0x0000002,
            VER_MINORVERSION = 0x0000001,
            VER_PLATFORMID = 0x0000008,
            VER_SERVICEPACKMAJOR = 0x0000020,
            VER_SERVICEPACKMINOR = 0x0000010,
            VER_SUITENAME = 0x0000040,
            VER_PRODUCT_TYPE = 0x0000080
        }

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        public static extern bool SetConsoleCtrlHandler(
            [In, MarshalAs(UnmanagedType.FunctionPtr)]PHANDLER_ROUTINE HandlerRoutine,
            bool Add);

        internal static bool IsWindows8OrGreater()
        {
            const ushort _WIN32_WINNT_WIN8 = 0x0602;
            return IsWindowsVersionOrGreater((byte)(_WIN32_WINNT_WIN8 >> 8), _WIN32_WINNT_WIN8 & 0xFF, 0);
        }

        static bool IsWindowsVersionOrGreater(byte wMajorVersion, byte wMinorVersion, ushort wServicePackMajor)
        {
            var osvi = new OSVERSIONINFOEX();
            osvi.dwOSVersionInfoSize = Marshal.SizeOf(osvi);
            osvi.dwMajorVersion = wMajorVersion;
            osvi.dwMinorVersion = wMinorVersion;
            osvi.wServicePackMajor = wServicePackMajor;

            var dwlConditionMask = VerSetConditionMask(
                VerSetConditionMask(
                    VerSetConditionMask(
                        0, TypeMask.VER_MAJORVERSION, ConditionMask.VER_GREATER_EQUAL),
                        TypeMask.VER_MINORVERSION, ConditionMask.VER_GREATER_EQUAL),
                        TypeMask.VER_SERVICEPACKMAJOR, ConditionMask.VER_GREATER_EQUAL);
            return VerifyVersionInfo(
                ref osvi,
                TypeMask.VER_MAJORVERSION | TypeMask.VER_MINORVERSION | TypeMask.VER_SERVICEPACKMAJOR,
                dwlConditionMask);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        static extern bool VerifyVersionInfo(
            [In] ref OSVERSIONINFOEX lpVersionInfo,
            TypeMask dwTypeMask,
            ulong dwlConditionMask);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        static extern ulong VerSetConditionMask(
            ulong dwlConditionMask,
            TypeMask dwTypeBitMask,
            ConditionMask dwConditionMask);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        struct OSVERSIONINFOEX
        {
            public int dwOSVersionInfoSize;
            public int dwMajorVersion;
            public int dwMinorVersion;
            public int dwBuildNumber;
            public int dwPlatformId;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;

            public ushort wServicePackMajor;
            public ushort wServicePackMinor;
            public ushort wSuiteMask;
            public byte wProductType;
            public byte wReserved;
        }
    }
}