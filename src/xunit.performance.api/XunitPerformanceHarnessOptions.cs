using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Xunit.Performance.Api
{
    /// <summary>
    /// Xunit Performance Harness Api command line options.
    /// </summary>
    internal sealed class XunitPerformanceHarnessOptions
    {
        public XunitPerformanceHarnessOptions()
        {
            _outputDirectory = Directory.GetCurrentDirectory();
            _temporaryDirectory = Path.GetTempPath();
            _runid = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
            _typeNames = new List<string>();
        }

        [Option("perf:outputdir", Required = false, HelpText = "Specifies the output directory name.")]
        public string OutputDirectory
        {
            get { return _outputDirectory; }

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new Exception("The output directory name cannot be null, empty or white space.");
                }

                if (value.Any(c => Path.GetInvalidPathChars().Contains(c)))
                {
                    throw new Exception("Specified output directory name contains invalid path characters.");
                }

                _outputDirectory = Path.IsPathRooted(value) ? value : Path.GetFullPath(value);
                if (!Directory.Exists(_outputDirectory))
                {
                    try 
                    {
                        Directory.CreateDirectory(_temporaryDirectory);
                    } catch (System.Exception) 
                    {
                        Console.Error.WriteLine("Couldn't create Directory " + _temporaryDirectory);
                        throw;
                    }
                }
            }
        }

        [Option("perf:tmpdir", Required = false, HelpText = "Specifies a writable directory to store temporary files.")]
        public string TemporaryDirectory
        {
            get { return _temporaryDirectory; }

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new Exception("The temporary directory name cannot be null, empty or white space.");
                }

                if (value.Any(c => Path.GetInvalidPathChars().Contains(c)))
                {
                    throw new Exception("Specified temporary directory name contains invalid path characters.");
                }

                _temporaryDirectory = Path.IsPathRooted(value) ? value : Path.GetFullPath(value);
                if (!Directory.Exists(_temporaryDirectory))
                {
                    try 
                    {
                        Directory.CreateDirectory(_temporaryDirectory);
                    } catch (System.Exception) 
                    {
                        Console.Error.WriteLine("Couldn't create Directory " + _temporaryDirectory);
                        throw;
                    }
                }
            }
        }

        [Option("perf:runid", Required = false, HelpText = "User defined id given to the performance harness.")]
        public string RunId
        {
            get { return _runid; }

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new Exception("The RunId cannot be null, empty or white space.");
                }

                if (value.Any(c => Path.GetInvalidFileNameChars().Contains(c)))
                {
                    throw new Exception("Specified RunId contains invalid file name characters.");
                }

                _runid = value;
            }
        }

        [Option("perf:typenames", Required = false, Separator = ',',
            HelpText = "The (optional) type names of the test classes to run.")]
        public IEnumerable<string> TypeNames
        {
            get { return _typeNames; }

            set
            {
                _typeNames.Clear();
                _typeNames.AddRange(value);
            }
        }

        public static XunitPerformanceHarnessOptions Parse(string[] args)
        {
            using (var parser = new Parser((settings) =>
            {
                settings.CaseInsensitiveEnumValues = true;
                settings.CaseSensitive = false;
                settings.HelpWriter = new StringWriter();
                settings.IgnoreUnknownArguments = true;
            }))
            {
                XunitPerformanceHarnessOptions options = null;
                var parserResult = parser.ParseArguments<XunitPerformanceHarnessOptions>(args)
                    .WithParsed(parsed => options = parsed)
                    .WithNotParsed(errors =>
                    {
                        foreach (var error in errors)
                        {
                            switch (error.Tag)
                            {
                                case ErrorType.MissingValueOptionError:
                                    throw new ArgumentException(
                                        $"Missing value option for {(error as MissingValueOptionError).NameInfo.NameText}");
                                case ErrorType.HelpRequestedError:
                                    Console.WriteLine(Usage());
                                    Environment.Exit(0);
                                    break;
                                case ErrorType.BadFormatTokenError:
                                case ErrorType.UnknownOptionError:
                                case ErrorType.MissingRequiredOptionError:
                                case ErrorType.MutuallyExclusiveSetError:
                                case ErrorType.BadFormatConversionError:
                                case ErrorType.SequenceOutOfRangeError:
                                case ErrorType.RepeatedOptionError:
                                case ErrorType.NoVerbSelectedError:
                                case ErrorType.BadVerbSelectedError:
                                case ErrorType.HelpVerbRequestedError:
                                    break;
                            }
                        }
                    });
                return options;
            }
        }

        public static string Usage()
        {
            var parser = new Parser((parserSettings) =>
            {
                parserSettings.CaseInsensitiveEnumValues = true;
                parserSettings.CaseSensitive = false;
                parserSettings.EnableDashDash = true;
                parserSettings.HelpWriter = new StringWriter();
                parserSettings.IgnoreUnknownArguments = true;
            });
            var result = parser.ParseArguments<XunitPerformanceHarnessOptions>(new string[] { "--help" });

            var helpTextString = new HelpText
            {
                AddDashesToOption = true,
                AddEnumValuesToHelpText = true,
                AdditionalNewLineAfterOption = false,
                Copyright = "Copyright (c) Microsoft Corporation 2015",
                Heading = "Xunit-Performance-Api",
                MaximumDisplayWidth = 80,
            }.AddOptions(result).ToString();
            return helpTextString;
        }


        private string _outputDirectory;
        private string _temporaryDirectory;
        private string _runid;
        private List<string> _typeNames;
    }
}
