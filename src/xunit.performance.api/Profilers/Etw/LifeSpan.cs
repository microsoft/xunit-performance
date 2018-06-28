// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Xunit.Performance.Api.Profilers.Etw
{
    /// <summary>
    /// Defines the length of time for which an object lives.
    /// </summary>
    public sealed class LifeSpan : IInterval<DateTime>, IEquatable<LifeSpan>
    {
        /// <summary>
        /// Lifetime duration.
        /// </summary>
        public TimeSpan Duration => End - Start;

        /// <summary>
        /// Gets the time associated with the object lifetime end.
        /// </summary>
        public DateTime End { get; set; } = DateTime.MaxValue;

        /// <summary>
        /// Gets the time associated with the object lifetime start.
        /// </summary>
        public DateTime Start { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Determines whether two specified <see cref="LifeSpan"/> objects have different values.
        /// </summary>
        /// <param name="lhs">The first <see cref="LifeSpan"/> to compare, or null.</param>
        /// <param name="rhs">The second <see cref="LifeSpan"/> to compare, or null.</param>
        /// <returns>True if the value of a is different from the value of b; otherwise, false.</returns>
        public static bool operator !=(LifeSpan lhs, LifeSpan rhs) => !(lhs == rhs);

        /// <summary>
        /// Determines whether two specified <see cref="LifeSpan"/> have the same value.
        /// </summary>
        /// <param name="lhs">The first <see cref="LifeSpan"/> to compare, or null.</param>
        /// <param name="rhs">The second <see cref="LifeSpan"/> to compare, or null.</param>
        /// <returns>True if its two operands refer to the same object or if the values of its operands are equal; otherwise, false.</returns>
        public static bool operator ==(LifeSpan lhs, LifeSpan rhs) => (object)lhs != null ? lhs.Equals(rhs) : (object)rhs == null;

        /// <summary>
        /// Determines whether the specified object is equals to this object.
        /// </summary>
        /// <param name="obj">The object to compare with this object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj) => Equals(obj as LifeSpan);

        /// <summary>
        /// Indicates whether the current object is equal to another <see cref="LifeSpan"/> object.
        /// </summary>
        /// <param name="other">A <see cref="LifeSpan"/> object to compare with this object.</param>
        /// <returns>true if the current object is equal to the other parameter; otherwise, false.</returns>
        public bool Equals(LifeSpan other)
        {
            if ((object)other == null)
                return false;
            return Start == other.Start && End == other.End;
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode() => Start.GetHashCode() ^ End.GetHashCode();

        /// <summary>
        /// Tests if a DateTime object is within the extents of this <see cref="LifeSpan"/> object.
        /// </summary>
        /// <param name="dt">DateTime object to test against this timespan.</param>
        /// <returns>True if dt is within the interval, otherwise false.</returns>
        public int IsInInterval(DateTime dt)
        {
            if (dt < Start)
                return -1;
            else if (dt >= End)
                return 1;
            else
                return 0;
        }
    }
}