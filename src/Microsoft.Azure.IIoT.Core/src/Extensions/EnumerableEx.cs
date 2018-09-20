// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Linq {
    using System;
    using System.Collections.Generic;
    using System.Collections;
    using System.Reflection;

    /// <summary>
    /// Enumerable extensions
    /// </summary>
    public static class EnumerableEx {

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
        /// Convert one item into an enumerable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static IEnumerable<T> CastTo<T>(this IEnumerable enumerable) {
            foreach (var item in enumerable) {
                yield return (T)item;
            }
        }

        /// <summary>
        /// Compares two enumerables
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable1"></param>
        /// <param name="enumerable2"></param>
        /// <returns></returns>
        public static bool SameAs<T>(this IEnumerable<T> enumerable1,
            IEnumerable<T> enumerable2) {
            return new HashSet<T>(enumerable1).SetEquals(enumerable2);
        }

        /// <summary>
        /// Try convert each item in enumerable
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<T> SelectAs<T>(this IEnumerable enumerable) =>
            SelectAs(enumerable, o => o.As<T>());

        /// <summary>
        /// Try convert each item in enumerable using a series of conversion
        /// attempts.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<T> SelectAs<T>(this IEnumerable enumerable,
            Func<object, T> converter) {
            foreach (var result in enumerable) {
                T value;
                if (typeof(T).IsAssignableFrom(result.GetType())) {
                    // Already assignable, no need to convert
                    value = (T)result;
                }
                else {
                    try {
                        // Try converting using the converter
                        value = converter(result);
                    }
                    catch {
                        try {
                            // Finally try any explicit conversion if they exist.
                            value = (T)result;
                        }
                        catch {
                            continue;
                        }
                    }
                }
                yield return value;
            }
        }

        /// <summary>
        /// Copies to an array of type type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Array ToArray<T>(this IEnumerable<T> enumerable, Type type) {
            if (enumerable == null) {
                return null;
            }
            var source = enumerable.ToArray();
            var target = Array.CreateInstance(type, source.Length);
            Array.Copy(source, target, source.Length);
            return target;
        }

        /// <summary>
        /// Returns one item or null for value types
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="checkSingle"></param>
        /// <returns></returns>
        public static T? OneOrNull<T>(this IEnumerable<T> source,
            bool checkSingle = false) where T : struct {
            if (source is IList<T> list) {
                if (list.Count == 0) {
                    return null;
                }
                if (checkSingle && list.Count != 1) {
                    throw new AmbiguousMatchException();
                }
                return list[0];
            }
            using (var e = source.GetEnumerator()) {
                if (!e.MoveNext()) {
                    return null;
                }
                var result = e.Current;
                if (checkSingle && e.MoveNext()) {
                    throw new AmbiguousMatchException();
                }
                return result;
            }
        }

        /// <summary>
        /// Returns one item or default (null for reference types)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="checkSingle"></param>
        /// <returns></returns>
        public static T OneOrDefault<T>(this IEnumerable<T> source,
            bool checkSingle = false) => source.OneOrThis(default(T), checkSingle);

        /// <summary>
        /// Returns one item or default (null for reference types)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="thiz"></param>
        /// <param name="checkSingle"></param>
        /// <returns></returns>
        public static T OneOrThis<T>(this IEnumerable<T> source,
            T thiz, bool checkSingle = false) {
            if (source is IList<T> list) {
                if (list.Count == 0) {
                    return thiz;
                }
                if (checkSingle && list.Count != 1) {
                    throw new AmbiguousMatchException();
                }
                return list[0];
            }
            using (var e = source.GetEnumerator()) {
                if (!e.MoveNext()) {
                    return thiz;
                }
                var result = e.Current;
                if (checkSingle && e.MoveNext()) {
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
    }
}
