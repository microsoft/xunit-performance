// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xunit.Performance.Api.Profilers.Etw
{
    /// <summary>
    /// Represents an address space as given by the Microsoft.Diagnostics.Tracing.Parsers
    /// </summary>
    internal sealed class AddressSpace : ISpan<ulong>
    {
        /// <summary>
        /// Initializes a new instance of the AddressSpace class.
        /// </summary>
        /// <param name="address">Interval's start address.</param>
        /// <param name="size">Interval's size.</param>
        public AddressSpace(ulong address, int size)
        {
            Start = address;
            Size = size;
            End = Start + (uint)Size;
        }

        /// <summary>
        /// The size of the address space.
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// The address where the module was loaded.
        /// </summary>
        public ulong Start { get; }

        /// <summary>
        /// Start + Size
        /// </summary>
        public ulong End { get; }

        /// <summary>
        /// Determines if the specified address falls within this address space.
        /// </summary>
        /// <param name="address">Address to test against this address space.</param>
        /// <returns>True if the address is within this interval, false otherwise.</returns>
        public bool IsInInterval(ulong address)
        {
            return (Start <= address) && (address < End);
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

            var addressSpace = obj as AddressSpace;
            if (addressSpace == null)
                return false;

            return Start == addressSpace.Start && Size == addressSpace.Size;
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return Start.GetHashCode() ^ Size;
        }

        /// <summary>
        /// Determines whether two specified AddressSpace have the same value.
        /// </summary>
        /// <param name="lhs">The first AddressSpace to compare, or null.</param>
        /// <param name="rhs">The second AddressSpace to compare, or null.</param>
        /// <returns>True if its two operands refer to the same object or if the values of its operands are equal; otherwise, false.</returns>
        public static bool operator ==(AddressSpace lhs, AddressSpace rhs)
        {
            if (ReferenceEquals(lhs, rhs))
                return true;

            if ((object)lhs == null || (object)rhs == null)
                return false;

            return lhs.Start == rhs.Start && lhs.Size == rhs.Size;
        }

        /// <summary>
        /// Determines whether two specified AddressSpace objects have different values.
        /// </summary>
        /// <param name="lhs">The first AddressSpace to compare, or null.</param>
        /// <param name="rhs">The second AddressSpace to compare, or null.</param>
        /// <returns>True if the value of a is different from the value of b; otherwise, false.</returns>
        public static bool operator !=(AddressSpace lhs, AddressSpace rhs)
        {
            return !(lhs == rhs);
        }
    }
}
