// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Linq
{
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// Enumerable extensions
    /// </summary>
    public static class LinqEx2
    {
        /// <summary>
        /// Merge enumerable b into set a.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static IReadOnlySet<T> MergeWith<T>(this IReadOnlySet<T> a, IEnumerable<T> b)
        {
            if (b?.Any() ?? false)
            {
                if (a == null)
                {
                    return b.ToHashSetSafe();
                }

                return a.Concat(b).ToHashSet();
            }
            return a;
        }

        /// <summary>
        /// Returns the contents of a dictionary as KeyValuePairs
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public static IEnumerable<KeyValuePair<TKey, TValue>> ToKeyValuePairs<TKey, TValue>(
            this IDictionary dictionary)
        {
            foreach (var key in dictionary.Keys)
            {
                yield return new KeyValuePair<TKey, TValue>((TKey)key, (TValue)dictionary[key]);
            }
        }

        /// <summary>
        /// Creates a hash set from enumerable or null if enumerable is null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static HashSet<T> ToHashSetSafe<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
            {
                return null;
            }
            return new HashSet<T>(enumerable);
        }

        /// <summary>
        /// Flattens a enumerable of enumerables
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static IEnumerable Flatten(this IEnumerable obj)
        {
            foreach (var item in obj)
            {
                if (item is IEnumerable contained)
                {
                    contained = contained.Flatten();
                    foreach (var cont in contained)
                    {
                        yield return cont;
                    }
                    yield break;
                }
                yield return item;
            }
        }

        /// <summary>
        /// Flattens a enumerable of typed enumerables
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> obj)
        {
            foreach (var contained in obj)
            {
                foreach (var cont in contained)
                {
                    yield return cont;
                }
            }
        }
    }
}
