using System;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Api
{
    internal static class FunctionalExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
        {
            foreach (T item in enumeration)
                action(item);
        }
    }
}
