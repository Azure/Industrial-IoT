// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Collections.Generic
{
    /// <summary>
    /// Dictionary extensions
    /// </summary>
    public static class DictionaryEx
    {
        /// <summary>
        /// Returns the contents of a dictionary as KeyValuePairs
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public static IEnumerable<KeyValuePair<K, V>> ToKeyValuePairs<K, V>(
            this IDictionary dictionary)
        {
            foreach (var key in dictionary.Keys)
            {
                yield return new KeyValuePair<K, V>((K)key, (V)dictionary[key]);
            }
        }
    }
}
