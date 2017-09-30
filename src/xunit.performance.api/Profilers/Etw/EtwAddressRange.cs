namespace Microsoft.Xunit.Performance.Api.Profilers.Etw
{
    internal class EtwAddressRange
    {
        /// <summary>
        /// Initializes a new instance of the EtwAddressRange struct.
        /// </summary>
        /// <param name="address">Range's start address.</param>
        /// <param name="size">Range's size.</param>
        public EtwAddressRange(ulong address, int size)
        {
            Size = size;
            Start = address;
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
        public ulong End => Start + (uint)Size;

        /// <summary>
        /// Determines if the specified address falls within this address range.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public bool IsWithinRange(ulong address)
        {
            return (Start <= address) && (address < End);
        }
    }
}
