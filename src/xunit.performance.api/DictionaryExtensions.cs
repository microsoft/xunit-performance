// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Xunit.Performance.Api
{
    static class DictionaryExtensions
    {
        public static void Add<TKey, TValue>(this IDictionary<TKey, List<TValue>> dictionary, TKey key, TValue value) => dictionary.GetOrAdd(key).Add(value);

        public static bool Contains<TKey, TValue>(this IDictionary<TKey, List<TValue>> dictionary, TKey key, TValue value, IEqualityComparer<TValue> valueComparer)
        {
            if (!dictionary.TryGetValue(key, out var values))
                return false;

            return values.Contains(value, valueComparer);
        }

        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
            where TValue : new() => dictionary.GetOrAdd<TKey, TValue>(key, () => new TValue());

        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> newValue)
        {
            if (!dictionary.TryGetValue(key, out var result))
            {
                result = newValue();
                dictionary[key] = result;
            }

            return result;
        }

        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue))
        {
            if (!dictionary.TryGetValue(key, out var value))
                return defaultValue;
            return value;
        }
    }
}