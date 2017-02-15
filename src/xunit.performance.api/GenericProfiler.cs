using System;

namespace Microsoft.Xunit.Performance.Api
{
    internal static class GenericProfiler
    {
        public static void Record(string assemblyFileName, string sessionName, string outputDirectory, Action action, Action<string> collectOutputFilesCallback)
        {
            action.Invoke();
        }
    }
}
