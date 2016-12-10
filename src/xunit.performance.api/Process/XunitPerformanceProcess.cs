using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.Xunit.Performance.Api.XunitPerformanceLogger;

namespace Microsoft.Xunit.Performance.Api
{
    internal static class XunitPerformanceProcess
    {
        public static string CurrentWorkingDirectory => Directory.GetCurrentDirectory();

        private static string[] GetProcessCommandLine(int processId)
        {
            // > WMIC.exe PROCESS WHERE ProcessId={Id} GET CommandLine /VALUE
            var file = "WMIC.exe";
            var wmicArgs = $"PROCESS WHERE ProcessId={processId} GET CommandLine /VALUE";
            var wmicOutputString = StdoutFrom(file, wmicArgs);
            var commandLine = ParseWmiCCommandLineString(wmicOutputString);
            return ArgParse.SplitCommandLine(commandLine);
        }

        private static string[] GetProcessCommandLine(Process process)
        {
            WriteDebugLine($"Process: {process.ToString()}");
            WriteDebugLine($"  ExecutablePath:   {process.MainModule.FileName}");
            WriteDebugLine($"  ProcessId: {process.Id}");
            WriteDebugLine($"  WorkingDirectory: {CurrentWorkingDirectory}");
            return GetProcessCommandLine(process.Id);
        }

        public static string[] GetProcessCommandLine()
        {
            return GetProcessCommandLine(Process.GetCurrentProcess());
        }

        public static string ParseWmiCCommandLineString(string commandLine)
        {
            const string pattern = "^CommandLine=(.+)$";
            const RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Compiled;
            var regex = new Regex(pattern, options);
            var match = regex.Match(commandLine);

            if (match.Success || match.Groups.Count == 2)
            {
                var wmicCmdLine = match.Groups[1].Value;
                return wmicCmdLine;
            }

            throw new ArgumentException($"{commandLine} does not contain the WMIC output `CommandLine=` pattern.");
        }

        /// <summary>
        /// Shells out, and if the process fails, log the error and quit the script.
        /// </summary>
        public static void ShellOutVital(
                string file,
                string args,
                string workingDirectory = null,
                CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = ShellOut(file, args, workingDirectory, cancellationToken);
            if (result.Failed)
            {
                LogProcessResult(result);
                throw new Exception("Shelling out failed");
            }
        }

        /// <summary>
        /// Shells out and returns the string gathered from the stdout of the
        /// executing process.
        ///
        /// Throws an exception if the process fails.
        /// </summary>
        public static string StdoutFrom(string program, string args = "", string workingDirectory = null)
        {
            var result = ShellOut(program, args, workingDirectory);
            if (result.Failed)
            {
                LogProcessResult(result);
                throw new Exception("Shelling out failed");
            }
            return result.StdOut.Trim();
        }

        /// <summary>
        /// Shells out, blocks, and returns the ProcessResult.
        /// </summary>
        public static XunitPerformanceProcessResult ShellOut(
                string file,
                string args,
                string workingDirectory = null,
                CancellationToken cancellationToken = default(CancellationToken),
                bool runElevated = false)
        {
            if (workingDirectory == null)
            {
                workingDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }

            var tcs = new TaskCompletionSource<XunitPerformanceProcessResult>();
            var startInfo = new ProcessStartInfo(file, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = workingDirectory
            };

            if (runElevated)
            {
                // FIXME: error CS1061: 'ProcessStartInfo' does not contain a definition for 'Verb' and no extension method 'Verb' accepting a first argument of type 'ProcessStartInfo' could be found (are you missing a using directive or an assembly reference?)
                // startInfo.Verb = "runas";
                Console.WriteLine("ERROR: Unable to run as admin at the moment.");
            }

            var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true,
            };

            if (cancellationToken != default(CancellationToken))
            {
                cancellationToken.Register(() => process.Kill());
            }

            if (XunitPerformanceRuntimeSettings.IsVerbose)
            {
                Console.WriteLine($"Running \"{file}\" with arguments \"{args}\" from directory {workingDirectory}");
            }

            process.Start();

            var output = new StringWriter();
            var error = new StringWriter();

            process.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    output.WriteLine(e.Data);
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    error.WriteLine(e.Data);
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            return new XunitPerformanceProcessResult
            {
                ExecutablePath = file,
                Args = args,
                Code = process.ExitCode,
                StdOut = output.ToString(),
                StdErr = error.ToString(),
            };
        }

        /// <summary>
        /// Logs the result of a finished process.
        /// </summary>
        public static void LogProcessResult(XunitPerformanceProcessResult result)
        {
            var outcome = result.Failed ? "failed" : "succeeded";
            Console.WriteLine($"The process \"{result.ExecutablePath} {result.Args}\" {outcome} with code {result.Code}.");
            Console.WriteLine($"Standard Out:");
            Console.WriteLine(result.StdOut);
            Console.WriteLine($"Standard Error:");
            Console.WriteLine(result.StdErr);
        }
    }
}
