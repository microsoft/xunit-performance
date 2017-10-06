namespace Microsoft.Xunit.Performance.Api.Profilers.Etw
{
    /// <summary>
    /// Represents an address range as given by the Microsoft.Diagnostics.Tracing.Parsers
    /// </summary>
    internal class EtwAddressRange
    {
        /// <summary>
        /// Initializes a new instance of the EtwAddressRange class.
        /// </summary>
        /// <param name="address">Range's start address.</param>
        /// <param name="size">Range's size.</param>
        public EtwAddressRange(ulong address, int size)
        {
            Start = address;
            Size = size;
            End = Start + (uint)Size;
        }

        /// <summary>
        /// The size of the address range.
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
        /// Determines if the specified address falls within this address range.
        /// </summary>
        /// <param name="address">Address to test against this address range.</param>
        /// <returns>True if the address is within range, false otherwise.</returns>
        public bool IsWithinRange(ulong address)
        {
            return (Start <= address) && (address < End);
        }
    }
}
