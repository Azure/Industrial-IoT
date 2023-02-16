// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Linq {
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// Enumerable extensions
    /// </summary>
    public static class LinqEx {

        /// <summary>
        /// Creates a hash set from enumerable or null if enumerable is null.
        /// </summary>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static HashSet<T> ToHashSetSafe<T>(this IEnumerable<T> enumerable) {
            if (enumerable == null) {
                return null;
            }
            return new HashSet<T>(enumerable);
        }

        /// <summary>
        /// Flattens a enumerable of enumerables
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static IEnumerable Flatten(this IEnumerable obj) {
            foreach (var item in obj) {
                if (item is IEnumerable contained) {
                    contained = contained.Flatten();
                    foreach (var cont in contained) {
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
        public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> obj) {
            foreach (var contained in obj) {
                foreach (var cont in contained) {
                    yield return cont;
                }
            }
        }

        /// <summary>
        /// Convert one item into an enumerable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static IEnumerable<T> YieldReturn<T>(this T obj) {
            yield return obj;
        }

        /// <summary>
        /// Creates an enumeration with created values
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="factory"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static IEnumerable<T> Repeat<T>(Func<T> factory, int count) {
            for (var i = 0; i < count; i++) {
                yield return factory();
            }
        }

        /// <summary>
        /// Creates an enumeration with created values
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="factory"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static IEnumerable<T> Repeat<T>(Func<int, T> factory, int count) {
            for (var i = 0; i < count; i++) {
                yield return factory(i);
            }
        }

        /// <summary>
        /// Create batches of enumerables
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> items,
            int count) {
            return items
                .Select((x, i) => Tuple.Create(x, i))
                .GroupBy(x => x.Item2 / count)
                .Select(g => g.Select(x => x.Item1));
        }
    }
}
