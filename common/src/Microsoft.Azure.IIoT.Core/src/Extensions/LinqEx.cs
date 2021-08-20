// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Linq {
    using System.Collections.Generic;
    using System.Collections;
    using System.Reflection;

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
        /// Creates a list from enumerable or null if enumerable is null.
        /// </summary>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static List<T> ToListSafe<T>(this IEnumerable<T> enumerable) {
            if (enumerable == null) {
                return null;
            }
            return new List<T>(enumerable);
        }

        /// <summary>
        /// Creates a sorted set from enumerable or null if enumerable is null.
        /// </summary>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static SortedSet<T> ToSortedSetSafe<T>(this IEnumerable<T> enumerable) {
            if (enumerable == null) {
                return null;
            }
            return new SortedSet<T>(enumerable);
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
        public static IEnumerable<T> Flatten<T>(this IEnumerable obj) {
            foreach (var item in obj) {
                if (item is IEnumerable contained) {
                    contained = contained.Flatten();
                    foreach (var cont in contained) {
                        yield return (T)cont;
                    }
                    yield break;
                }
                yield return (T)item;
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
        /// Returns one item or default (null for reference types)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="checkSingle"></param>
        /// <returns></returns>
        public static T OneOrDefault<T>(this IEnumerable<T> source,
            bool checkSingle = false) {
            return source.OneOrThis(default, checkSingle);
        }

        /// <summary>
        /// Returns one item or default (null for reference types)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="thiz"></param>
        /// <param name="throwIfMoreThanOne">throw if more than one
        /// item in the source enumeration</param>
        /// <returns></returns>
        public static T OneOrThis<T>(this IEnumerable<T> source,
            T thiz, bool throwIfMoreThanOne = false) {
            if (source == null) {
                return thiz;
            }
            if (source is IList<T> list) {
                if (list.Count == 0) {
                    return thiz;
                }
                if (throwIfMoreThanOne && list.Count != 1) {
                    throw new AmbiguousMatchException();
                }
                return list[0];
            }
            using (var e = source.GetEnumerator()) {
                if (!e.MoveNext()) {
                    return thiz;
                }
                var result = e.Current;
                if (throwIfMoreThanOne && e.MoveNext()) {
                    throw new AmbiguousMatchException();
                }
                return result;
            }
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
