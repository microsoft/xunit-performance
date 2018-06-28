// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Xunit.Performance.Sdk
{
    /// <summary>
    /// An attribute used to decorate classes which implement <see cref="IPerformanceMetricAttribute"/>, to indicate
    /// how performance metrics should be discovered.  The discoverer type must implement <see cref="IPerformanceMetricDiscoverer"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class PerformanceMetricDiscovererAttribute : Attribute
    {
        //
        // Summary:
        //     Initializes an instance of Xunit.Sdk.TraitDiscovererAttribute.
        //
        // Parameters:
        //   typeName:
        //     The fully qualified type name of the discoverer (f.e., 'Xunit.Sdk.TraitDiscoverer')
        //
        //   assemblyName:
        //     The name of the assembly that the discoverer type is located in, without file
        //     extension (f.e., 'xunit.execution')
        /// <summary>
        /// Initializes a new instance of <see cref="PerformanceMetricDiscovererAttribute"/>.
        /// </summary>
        /// <param name="typeName">The fully qualified name of the discoverer.</param>
        /// <param name="assemblyName">The name of the assembly that the discoverer type is located in, without file extension.</param>
        public PerformanceMetricDiscovererAttribute(string typeName, string assemblyName)
        {
        }
    }
}