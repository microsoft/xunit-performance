// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Xunit.Performance
{
    public class XunitPerformanceProject : XunitProject
    {
        private List<XunitProjectAssembly> _baselineAssemblies = new List<XunitProjectAssembly>();
        private string _baselineRunnerCommand;

        public IEnumerable<XunitProjectAssembly> BaselineAssemblies { get { return _baselineAssemblies; } }

        public void AddBaseline(XunitProjectAssembly assembly) { _baselineAssemblies.Add(assembly); }

        public string RunnerHost { get; set; } = null;

        public string RunnerCommand { get; set; } = "xunit.console.exe";

        public string RunnerArgs { get; set; }

        public string BaselineRunnerCommand
        {
            get { return _baselineRunnerCommand ?? RunnerCommand; }
            set { _baselineRunnerCommand = value; }
        }

        public string RunId { get; set; } = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss");

        public string OutputDir { get; set; } = ".";

        private string _outputBaseFileName;
        public string OutputBaseFileName {
            get
            {
                return _outputBaseFileName ?? RunId;
            }
            set
            {
                _outputBaseFileName = value;
            }
        }
    }
}
