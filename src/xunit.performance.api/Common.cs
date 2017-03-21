using System.Runtime.InteropServices;

namespace Microsoft.Xunit.Performance.Api
{
    /// <summary>
    /// This class provides common utilities to the API.
    /// </summary>
    internal static class Common
    {
        public static bool IsWindowsPlatform => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }
}
