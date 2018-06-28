// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance.Sdk;
using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;

namespace Microsoft.Xunit.Performance
{
    partial class InstructionsRetiredMetricDiscoverer : IPerformanceMetricDiscoverer
    {
        public const string CounterName = "InstructionRetired";
        public const int DefaultInterval = 1000000; // Instructions per event.

        public InstructionsRetiredMetricDiscoverer() => ProfileSource = PerformanceMetric.GetProfileSourceInfoId(CounterName);

        int ProfileSource { get; }

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