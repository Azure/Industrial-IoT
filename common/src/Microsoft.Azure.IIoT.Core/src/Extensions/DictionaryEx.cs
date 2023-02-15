// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Collections.Generic {
    using System.Linq;

    /// <summary>
    /// Dictionary extensions
    /// </summary>
    public static class DictionaryEx {

        /// <summary>
        /// Safe dictionary equals
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="dict"></param>
        /// <param name="that"></param>
        /// <param name="equality"></param>
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
        /// Returns the contents of a dictionary as KeyValuePairs
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public static IEnumerable<KeyValuePair<K, V>> ToKeyValuePairs<K, V>(
            this IDictionary dictionary) {
            foreach (var key in dictionary.Keys) {
                yield return new KeyValuePair<K, V>((K)key, (V)dictionary[key]);
            }
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
            return DictionaryEqualsSafe(dict, that, (x, y) => x.EqualsSafe(y));
        }

        /// <summary>
        /// Add or update item
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void AddOrUpdate<K, V>(this IDictionary<K, V> dict,
            K key, V value) {
            if (dict.ContainsKey(key)) {
                dict[key] = value;
            }
            else {
                dict.Add(key, value);
            }
        }
    }
}
