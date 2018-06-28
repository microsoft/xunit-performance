using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using static Microsoft.Xunit.Performance.Api.PerformanceLogger;

namespace Microsoft.Xunit.Performance.Api
{
    /// <summary>
    /// Xunit Performance Harness Api command line options.
    /// </summary>
    sealed partial class XunitPerformanceHarnessOptions
    {
        readonly List<string> _typeNames;

        IEnumerable<string> _metricNames;

        string _outputDirectory;

        string _runid;

        public XunitPerformanceHarnessOptions()
        {
            _metricNames = new List<string> { "default" };
            _outputDirectory = Directory.GetCurrentDirectory();
            _runid = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
            _typeNames = new List<string>();
        }

        [Option("perf:collect", Required = false, Separator = '+', Hidden = true,
            HelpText = "The metrics to be collected.")]
        public IEnumerable<string> MetricNames
        {
            get => _metricNames;

            set
            {
                var validCollectionOptions = new[] {
                    "default",
                    "gcapi",
                    "stopwatch",
                    "BranchMispredictions",
                    "CacheMisses",
                    "InstructionRetired",
                };
                var reducedList = value.Distinct(StringComparer.OrdinalIgnoreCase);
                var isSubset = !reducedList.Except(validCollectionOptions, StringComparer.OrdinalIgnoreCase).Any();

                if (!isSubset)
                {
                    var errorMessage = $"Valid collection metrics are: {string.Join("|", validCollectionOptions)}";
                    WriteErrorLine(errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }

                _metricNames = reducedList.Any() ? new List<string>(reducedList) : new List<string> { "default" };

                /*
                 * Dictionary<string, Factory>
                 */
            }
        }

        [Option("perf:outputdir", Required = false, HelpText = "Specifies the output directory name.")]
        public string OutputDirectory
        {
            get => _outputDirectory;

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new InvalidOperationException("The output directory name cannot be null, empty or white space.");
                }

                if (value.Any(c => Path.GetInvalidPathChars().Contains(c)))
                {
                    throw new InvalidOperationException("Specified output directory name contains invalid path characters.");
                }

                _outputDirectory = Path.IsPathRooted(value) ? value : Path.GetFullPath(value);
                Directory.CreateDirectory(_outputDirectory);
            }
        }

        [Option("perf:runid", Required = false, HelpText = "User defined id given to the performance harness.")]
        public string RunId
        {
            get => _runid;

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
            get => _typeNames;

            set
            {
                _typeNames.Clear();
                _typeNames.AddRange(value);
            }
        }

        /*
         * Provider & Reader
         *
         *  --perf:collect [metric1[+metric2[+...]]]
         *
         *    default
         *      Set by the test author (This is the default behavior if no option is specified. It will also enable ETW to capture some of the Microsoft-Windows-DotNETRuntime tasks).
         *
         *    stopwatch
         *      Capture elapsed time using a Stopwatch (It does not require ETW).
         *
         *    BranchMispredictions|CacheMisses|InstructionRetired
         *      These are performance metric counters and require ETW.
         *
         *    gcapi
         *      It currently enable "Allocation Size on Benchmark Execution Thread" and it is only available through ETW.
         *
         *  Examples
         *    --perf:collect default
         *      Collect metrics specified in the test source code by using xUnit Performance API attributes
         *
         *    --perf:collect BranchMispredictions+CacheMisses+InstructionRetired
         *      Collects PMC metrics
         *
         *    --perf:collect stopwatch
         *      Collects duration
         *
         *    --perf:collect default+BranchMispredictions+CacheMisses+InstructionRetired+gcapi
         *      '+' implies union of all specified options
         */

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

                                case ErrorType.VersionRequestedError:
                                    Console.WriteLine(new AssemblyName(typeof(XunitPerformanceHarnessOptions).GetTypeInfo().Assembly.FullName).Version);
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
    }
}