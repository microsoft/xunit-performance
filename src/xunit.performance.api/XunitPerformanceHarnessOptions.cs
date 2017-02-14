using CommandLine;
using System;
using System.IO;

namespace Microsoft.Xunit.Performance.Api
{
    sealed class XunitPerformanceHarnessOptions
    {
        static XunitPerformanceHarnessOptions()
        {
            s_outputDirectory = Directory.GetCurrentDirectory();
        }

        [Option("p:outputdir", Required = false, HelpText = "Specifies the output directory name.")]
        public string OutputDirectory
        {
            get => s_outputDirectory;
            set
            {
                s_outputDirectory = value;

                if (string.IsNullOrWhiteSpace(s_outputDirectory)) // TODO: Add better error handling.
                {
                    throw new Exception("Output directory cannot be null or white space.");
                }

                if (!Directory.Exists(s_outputDirectory))
                {
                    Directory.CreateDirectory(s_outputDirectory);
                }
            }
        }

        [Option("p:runid", Required = false, HelpText = "User defined id given to this harness. This is used to give the output files a name.")]
        public string RunId { get; set; }

        private static string s_outputDirectory;
    }
}
