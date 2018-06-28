// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Xunit.Performance.Api
{
    /// <summary>
    /// Defines an interval with a mechanism for testing if a value falls in the range.
    /// </summary>
    /// <typeparam name="T">The type interval space.</typeparam>
    interface IInterval<T>
        where T : IComparable, IComparable<T>
    {
        /// <summary>
        /// End limit of the interval.
        /// </summary>
        T End { get; }

        /// <summary>
        /// Start limit of the interval.
        /// </summary>
        T Start { get; }

        /// <summary>
        /// Determines if a value falls in this range.
        /// </summary>
        /// <param name="value">Value to test against this interval.</param>
        /// <returns>Less than zero, if value precedes Start. Greater than zero, if value follows End. Othewise, zero.</returns>
        int IsInInterval(T value);
    }
}