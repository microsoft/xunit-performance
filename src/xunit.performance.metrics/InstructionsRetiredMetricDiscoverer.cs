// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance
{
    internal partial class InstructionsRetiredMetricDiscoverer : IPerformanceMetricDiscoverer
    {
        public const int DefaultInterval = 100000; // Instructions per event.
        public const string CounterName = "InstructionRetired";

        private int ProfileSource { get; }

        public InstructionsRetiredMetricDiscoverer()
        {
            ProfileSource = PerformanceMetric.GetProfileSourceInfoId(CounterName);
        }

        public IEnumerable<PerformanceMetricInfo> GetMetrics()
        {
            if (ProfileSource != -1)
            {
                yield return new InstructionsRetiredMetric(DefaultInterval, ProfileSource);
            }
        }

        public IEnumerable<PerformanceMetricInfo> GetMetrics(IAttributeInfo metricAttribute)
        {
            if (ProfileSource != -1)
            {
                var interval = (int)(metricAttribute.GetConstructorArguments().FirstOrDefault() ?? DefaultInterval);
                yield return new InstructionsRetiredMetric(interval, ProfileSource);
            }
        }
    }
}
