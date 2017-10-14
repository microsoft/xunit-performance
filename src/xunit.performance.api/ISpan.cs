using System;

namespace Microsoft.Xunit.Performance.Api
{
    internal interface ISpan<T>
        where T : IComparable, IComparable<T>
    {
        T Start { get; }

        T End { get; }

        bool IsInInterval(T value);
    }
}
