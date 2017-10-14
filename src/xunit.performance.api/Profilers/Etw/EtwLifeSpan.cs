using System;

namespace Microsoft.Xunit.Performance.Api.Profilers.Etw
{
    public sealed class EtwLifeSpan : ISpan<DateTime>
    {
        public DateTime Start { get; set; } = DateTime.MinValue;

        public DateTime End { get; set; } = DateTime.MaxValue;

        public TimeSpan Duration => End - Start;

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

            var lifeSpan = obj as EtwLifeSpan;
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
        /// Determines whether two specified EtwLifeSpan have the same value.
        /// </summary>
        /// <param name="lhs">The first EtwLifeSpan to compare, or null.</param>
        /// <param name="rhs">The second EtwLifeSpan to compare, or null.</param>
        /// <returns>True if its two operands refer to the same object or if the values of its operands are equal; otherwise, false.</returns>
        public static bool operator ==(EtwLifeSpan lhs, EtwLifeSpan rhs)
        {
            if (ReferenceEquals(lhs, rhs))
                return true;

            if ((object)lhs == null || (object)rhs == null)
                return false;

            return lhs.Start == rhs.Start && lhs.End == rhs.End;
        }

        /// <summary>
        /// Determines whether two specified EtwLifeSpans have different values.
        /// </summary>
        /// <param name="lhs">The first EtwLifeSpan to compare, or null.</param>
        /// <param name="rhs">The second EtwLifeSpan to compare, or null.</param>
        /// <returns>True if the value of a is different from the value of b; otherwise, false.</returns>
        public static bool operator !=(EtwLifeSpan lhs, EtwLifeSpan rhs)
        {
            return !(lhs == rhs);
        }
    }
}
