using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Microsoft.Xunit.Performance.Api
{
    /// <summary>
    /// This class provides common utilities to the API.
    /// </summary>
    internal static class Common
    {
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// TODO: Revisit checking if admin for non-Windows platform.
        /// </remarks>
        public static bool IsRunningAsAdministrator
        {
            get
            {
                using (var identity = WindowsIdentity.GetCurrent())
                {
                    var principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
        }

        public static bool IsWindowsPlatform => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }
}
