// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Collections.Generic {
    using System;
    using System.Linq;

    public static class DictionaryEx {

        /// <summary>
        /// Safe dictionary equals
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="dict"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool DictionaryEqualsSafe<K, V>(this IDictionary<K, V> dict,
            IDictionary<K, V> that, Func<V, V, bool> equality) {
            if (dict == that) {
                return true;
            }
            if (dict == null || that == null) {
                return false;
            }
            if (dict.Count != that.Count) {
                return false;
            }
            return that.All(kv => dict.TryGetValue(kv.Key, out var v) &&
                equality(kv.Value, v));
        }

        /// <summary>
        /// Safe dictionary equals
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="dict"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool DictionaryEqualsSafe<K, V>(this IDictionary<K, V> dict,
            IDictionary<K, V> that) {
            return DictionaryEqualsSafe(dict, that, (v1, v2) => v1.EqualsSafe(v2));
        }

        /// <summary>
        /// Add or update item
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void AddOrUpdate<K, V>(this IDictionary<K, V> dictionary,
            K key, V value) {
            if (dictionary.ContainsKey(key)) {
                dictionary[key] = value;
            }
            else {
                dictionary.Add(key, value);
            }
        }

        /// <summary>
        /// Get or create new item
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static V GetOrCreate<K, V>(this IDictionary<K, V> dictionary, K key)
            where V : new() {
            if (dictionary.TryGetValue(key, out var result)) {
                return result;
            }
            result = new V();
            dictionary[key] = result;
            return result;
        }

        /// <summary>
        /// Add range to dictionary
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="enumerable"></param>
        public static void AddRange<K, V>(this IDictionary<K, V> dictionary,
            IEnumerable<KeyValuePair<K, V>> enumerable) {
            foreach (var item in enumerable) {
                dictionary.Add(item);
            }
        }
    }
}
