using System;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Api
{
    internal static class FunctionalExtensions
    {
        /// <summary>
        /// Performs the specified action on each element of the IEnumerable&lt;T&gt;.
        /// </summary>
        /// <typeparam name="T">The type of elements in the enumeration.</typeparam>
        /// <param name="enumeration">An IEnumerable&lt;T&gt; to perform the specified action on each element.</param>
        /// <param name="action">The Action&lt;T&gt; delegate to perform on each element of the IEnumerable&lt;T&gt;.</param>
        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
        {
            foreach (T item in enumeration)
                action(item);
        }

        /// <summary>
        /// Creates a HashSet&lt;T&gt; from an IEnumerable&lt;T&gt;.
        /// </summary>
        /// <typeparam name="T">The type of the elements of source.</typeparam>
        /// <param name="source">An IEnumerable&lt;T&gt; to create a HashSet&lt;T&gt; from.</param>
        /// <returns>A HashSet&lt;T&gt; that contains a unique set of values of the source.</returns>
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }
    }
}
