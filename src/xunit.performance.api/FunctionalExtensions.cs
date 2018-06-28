using System;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Api
{
    static class FunctionalExtensions
    {
        /// <summary>
        /// Performs the specified action on each element of the <see cref="System.Collections.Generic.IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements in the enumeration.</typeparam>
        /// <param name="enumeration">An <see cref="System.Collections.Generic.IEnumerable{T}"/> to perform the specified action on each element.</param>
        /// <param name="action">The <see cref="System.Action{T}"/> delegate to perform on each element of the <see cref="System.Collections.Generic.IEnumerable{T}"/>.</param>
        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
        {
            foreach (T item in enumeration)
                action(item);
        }

        /// <summary>
        /// Creates a <see cref="System.Collections.Generic.HashSet{T}"/> from an <see cref="System.Collections.Generic.IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements of source.</typeparam>
        /// <param name="source">An <see cref="System.Collections.Generic.IEnumerable{T}"/> to create a <see cref="System.Collections.Generic.HashSet{T}"/> from.</param>
        /// <returns>A <see cref="System.Collections.Generic.HashSet{T}"/> that contains a unique set of values of the source.</returns>
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source) => new HashSet<T>(source);
    }
}