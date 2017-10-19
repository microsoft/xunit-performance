// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Xunit.Performance.Api.Profilers.Etw
{
    /// <summary>
    /// Defines the length of time for which an object lives.
    /// </summary>
    public sealed class LifeSpan : IInterval<DateTime>
    {
        /// <summary>
        /// Gets the time associated with the object lifetime start.
        /// </summary>
        public DateTime Start { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Gets the time associated with the object lifetime end.
        /// </summary>
        public DateTime End { get; set; } = DateTime.MaxValue;

        /// <summary>
        /// Lifetime duration.
        /// </summary>
        public TimeSpan Duration => End - Start;

        /// <summary>
        /// Tests if a DateTime object is within the extents of this lifespan.
        /// </summary>
        /// <param name="dt">DateTime object to test against this timespan.</param>
        /// <returns>True if dt is within the interval, otherwise false.</returns>
        public bool IsInInterval(DateTime dt)
        {
            return (Start <= dt) && (dt < End);
        }

        /// <summary>
        /// Determines whether the specified object is equals to this object.
        /// </summary>
        /// <param name="obj">The object to compare with this object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var lifeSpan = obj as LifeSpan;
            if (lifeSpan == null)
                return false;

            return Start == lifeSpan.Start && End == lifeSpan.End;
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return Start.GetHashCode() ^ End.GetHashCode();
        }

        /// <summary>
        /// Determines whether two specified LifeSpan have the same value.
        /// </summary>
        /// <param name="lhs">The first LifeSpan to compare, or null.</param>
        /// <param name="rhs">The second LifeSpan to compare, or null.</param>
        /// <returns>True if its two operands refer to the same object or if the values of its operands are equal; otherwise, false.</returns>
        public static bool operator ==(LifeSpan lhs, LifeSpan rhs)
        {
            if (ReferenceEquals(lhs, rhs))
                return true;

            if ((object)lhs == null || (object)rhs == null)
                return false;

            return lhs.Start == rhs.Start && lhs.End == rhs.End;
        }

        /// <summary>
        /// Determines whether two specified LifeSpan objects have different values.
        /// </summary>
        /// <param name="lhs">The first LifeSpan to compare, or null.</param>
        /// <param name="rhs">The second LifeSpan to compare, or null.</param>
        /// <returns>True if the value of a is different from the value of b; otherwise, false.</returns>
        public static bool operator !=(LifeSpan lhs, LifeSpan rhs)
        {
            return !(lhs == rhs);
        }
    }
}
