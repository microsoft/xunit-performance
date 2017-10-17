// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Xunit.Performance.Api
{
    /// <summary>
    /// Defines an interval with a mechanism for testing if a value falls in the range.
    /// </summary>
    /// <typeparam name="T">The type span space.</typeparam>
    internal interface ISpan<T>
        where T : IComparable, IComparable<T>
    {
        /// <summary>
        /// Start limit of the span.
        /// </summary>
        T Start { get; }

        /// <summary>
        /// End limit of the span.
        /// </summary>
        T End { get; }

        /// <summary>
        /// Determines if a value falls in this range.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        bool IsInInterval(T value);
    }
}
