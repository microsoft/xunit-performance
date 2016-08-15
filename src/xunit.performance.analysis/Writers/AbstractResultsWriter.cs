// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.Xunit.Performance.Analysis
{
    internal abstract class AbstractResultsWriter
    {
        protected AbstractResultsWriter(
                            Dictionary<string, MetricInfo> allMetrics,
                            Dictionary<string, Dictionary<string, TestResult>> testResults,
                            Dictionary<string, List<TestResultComparison>> comparisonResults,
                            string outputPath)
        {
            this.AllMetrics = allMetrics;
            this.TestResults = testResults;
            this.ComparisonResults = comparisonResults;
            this.OutputPath = outputPath;

        }

        internal void Write()
        {
            using (this.OutputStream = new StreamWriter( new FileStream(this.OutputPath, FileMode.Create), Encoding.UTF8))
            {
                WriteHeader();

                WriteStatistics();

                WriteIndividualResults();

                WriteFooter();
            }
        }


        protected abstract void WriteHeader();

        protected abstract void WriteFooter();

        protected abstract void WriteStatistics();

        protected abstract void WriteIndividualResults();

        protected Dictionary<string, MetricInfo> AllMetrics { get; private set; }

        protected Dictionary<string, List<TestResultComparison>> ComparisonResults { get; private set; }

        protected Dictionary<string, Dictionary<string, TestResult>> TestResults { get; private set; }

        protected string OutputPath { get; private set; }

        protected StreamWriter OutputStream { get; private set; }
    }
}
