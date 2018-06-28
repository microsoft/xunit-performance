// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Xunit.Performance.Api.Profilers.Etw
{
    /// <summary>
    /// Represents an address space as given by the Microsoft.Diagnostics.Tracing.Parsers
    /// </summary>
    sealed class AddressSpace : IInterval<ulong>, IEquatable<AddressSpace>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AddressSpace"/> class.
        /// </summary>
        /// <param name="address">Interval's start address.</param>
        /// <param name="size">Interval's size.</param>
        public AddressSpace(ulong address, uint size)
        {
            Start = address;
            Size = size;
            End = Start + Size;
        }

        /// <summary>
        /// Start + Size
        /// </summary>
        public ulong End { get; }

        /// <summary>
        /// The size of the address space.
        /// </summary>
        public uint Size { get; }

        /// <summary>
        /// The address where the module was loaded.
        /// </summary>
        public ulong Start { get; }

        /// <summary>
        /// Determines whether two specified <see cref="AddressSpace"/> objects have different values.
        /// </summary>
        /// <param name="lhs">The first <see cref="AddressSpace"/> to compare, or null.</param>
        /// <param name="rhs">The second <see cref="AddressSpace"/> to compare, or null.</param>
        /// <returns>True if the value of a is different from the value of b; otherwise, false.</returns>
        public static bool operator !=(AddressSpace lhs, AddressSpace rhs) => !(lhs == rhs);

        /// <summary>
        /// Determines whether two specified <see cref="AddressSpace"/> have the same value.
        /// </summary>
        /// <param name="lhs">The first <see cref="AddressSpace"/> to compare, or null.</param>
        /// <param name="rhs">The second <see cref="AddressSpace"/> to compare, or null.</param>
        /// <returns>True if its two operands refer to the same object or if the values of its operands are equal; otherwise, false.</returns>
        public static bool operator ==(AddressSpace lhs, AddressSpace rhs) => (object)lhs != null ? lhs.Equals(rhs) : (object)rhs == null;

        /// <summary>
        /// Determines whether the specified object is equals to this object.
        /// </summary>
        /// <param name="obj">The object to compare with this object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj) => Equals(obj as AddressSpace);

        /// <summary>
        /// Indicates whether the current object is equal to another <see cref="AddressSpace"/> object.
        /// </summary>
        /// <param name="other">An <see cref="AddressSpace"/> object to compare with this object.</param>
        /// <returns>true if the current object is equal to the other parameter; otherwise, false.</returns>
        public bool Equals(AddressSpace other)
        {
            if ((object)other == null)
                return false;
            return Start == other.Start && Size == other.Size;
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode() => Start.GetHashCode() ^ Size.GetHashCode();

        /// <summary>
        /// Determines if the specified address falls within this address space.
        /// </summary>
        /// <param name="address">Address to test against this address space.</param>
        /// <returns>True if the address is within this interval, false otherwise.</returns>
        public int IsInInterval(ulong address)
        {
            if (address < Start)
                return -1;
            else if (address >= End)
                return 1;
            else
                return 0;
        }
    }
}