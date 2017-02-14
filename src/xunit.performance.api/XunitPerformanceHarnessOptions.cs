using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Microsoft.Xunit.Performance.Api.XunitPerformanceLogger;

namespace Microsoft.Xunit.Performance.Api
{
    /// <summary>
    /// Xunit Performance Harness Api command line options.
    /// </summary>
    [Verb("perf-api", HelpText = "Performance API options.")]
    internal sealed class XunitPerformanceHarnessOptions
    {
        public XunitPerformanceHarnessOptions()
        {
            _outputDirectory = Directory.GetCurrentDirectory();
            _runid = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
            _typeNames = new List<string>();
        }

        [Option('o', "outputdir", Required = false, HelpText = "Specifies the output directory name.")]
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
                    Directory.CreateDirectory(_outputDirectory);
                }
            }
        }

        [Option("id", Required = false, HelpText = "User defined id given to the performance harness.")]
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

        [Option('t', "typenames", Required = false, Separator = ',',
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
            XunitPerformanceHarnessOptions options = null;
            var parser = new Parser((parserSettings) =>
            {
                parserSettings.CaseInsensitiveEnumValues = true;
                parserSettings.CaseSensitive = false;
                parserSettings.EnableDashDash = true;
                //parserSettings.IgnoreUnknownArguments = true;
            });
            var parserResult = parser.ParseArguments<XunitPerformanceHarnessOptions>(args)
                .WithParsed(parsed => options = parsed)
                .WithNotParsed(errors =>
                {
                    foreach (var error in errors)
                    {
                        switch (error.Tag)
                        {
                            case ErrorType.MissingValueOptionError:
                                WriteErrorLine($"Missing value option for {(error as MissingValueOptionError).NameInfo.NameText}");
                                Environment.Exit(1);
                                break;
                            case ErrorType.HelpRequestedError:
                                Console.WriteLine(Usage());
                                Environment.Exit(0);
                                break;
                            case ErrorType.VersionRequestedError:
                                Console.WriteLine("TODO"); // TODO: Get this assembly version.
                                //(typeof(Microsoft.Xunit.Performance.Api.XunitPerformanceHarness) as System.RuntimeType).Assembly.Location
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

        public static string Usage()
        {
            var helpText = new HelpText
            {
                AddDashesToOption = true,
                AddEnumValuesToHelpText = true,
                AdditionalNewLineAfterOption = false,
                Copyright = "Copyright (c) Microsoft Corporation 2015",
                Heading = "Xunit-Performance-Api",
                MaximumDisplayWidth = 80
            };

            var helpWriter = new StringWriter();
            var parser = new Parser((parserSettings) =>
            {
                parserSettings.CaseInsensitiveEnumValues = true;
                parserSettings.CaseSensitive = false;
                parserSettings.EnableDashDash = true;
                parserSettings.HelpWriter = helpWriter;
                parserSettings.IgnoreUnknownArguments = true;
            });
            var result = parser.ParseArguments(new string[] { "perf-api", "--help" }, typeof(XunitPerformanceHarnessOptions));
            helpText.AddOptions(result);

            var helpTextString = helpText.ToString();
            return helpTextString;
        }


        private string _outputDirectory;
        private string _runid;
        private List<string> _typeNames;
    }
}
