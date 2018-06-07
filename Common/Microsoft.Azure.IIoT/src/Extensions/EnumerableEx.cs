// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Collections.Generic {
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Reflection;

    public static class EnumerableEx {

        /// <summary>
        /// Shuffle list
        /// </summary>
        private static readonly Random rng = new Random();
        public static IList<T> Shuffle<T>(this IList<T> list) {
            var n = list.Count;
            while (n > 1) {
                n--;
                var k = rng.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
            return list;
        }

        /// <summary>
        /// Safe sequence equals
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="seq"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool SequenceEqualsSafe<T>(this IEnumerable<T> seq, IEnumerable<T> that) {
            if (seq == that) {
                return true;
            }
            if (seq == null || that == null) {
                return false;
            }
            return seq.SequenceEqual(that);
        }

        /// <summary>
        /// Safe set equals
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="seq"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool SetEqualsSafe<T>(this ISet<T> seq, IEnumerable<T> that) {
            if (seq == that) {
                return true;
            }
            if (seq == null || that == null) {
                return false;
            }
            return seq.SetEquals(that);
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

        /// <summary>
        /// Adds enum range to list
        /// </summary>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static void AddRange(this IList list, IEnumerable enumerable) {
            foreach (var item in enumerable) {
                list.Add(item);
            }
        }

        /// <summary>
        /// Adds enum range to list
        /// </summary>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static void AddRange(this IList<object> list, IEnumerable enumerable) {
            foreach (var item in enumerable) {
                list.Add(item);
            }
        }

        /// <summary>
        /// Adds enum range to list
        /// </summary>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static void AddRange<T>(this IList<T> list, IEnumerable<T> enumerable) {
            foreach (var item in enumerable) {
                list.Add(item);
            }
        }

        /// <summary>
        /// Add range to set
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="set"></param>
        /// <param name="enumerable"></param>
        public static void AddRange<T>(this ISet<T> set, IEnumerable<T> enumerable) {
            foreach (var item in enumerable) {
                set.Add(item);
            }
        }

        /// <summary>
        /// Adds enum range to list
        /// </summary>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static void AddRange(this ISet<object> set, IEnumerable enumerable) {
            foreach (var item in enumerable) {
                set.Add(item);
            }
        }

        /// <summary>
        /// Add range to producer consumer collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="enumerable"></param>
        public static void AddRange<T>(this IProducerConsumerCollection<T> collection,
            IEnumerable<T> enumerable) {
            foreach (var item in enumerable) {
                while (!collection.TryAdd(item)) {
                }
            }
        }

        /// <summary>
        /// Return a set from enumerable
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
        /// Merge
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static HashSet<T> UnionWithSafe<T>(this HashSet<T> a, IEnumerable<T> b) {
            if (b?.Any() ?? false) {
                if (a == null) {
                    a = b.ToHashSetSafe();
                }
                else {
                    a.AddRange(b);
                }
            }
            return a;
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

        /// <summary>
        /// Pops a number of items from the queue
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queue"></param>
        /// <param name="count"></param>
        public static IEnumerable<T> Dequeue<T>(this Queue<T> queue, int count) {
            for (var i = 0; i < count && queue.Count != 0; i++) {
                yield return queue.Dequeue();
            }
        }

        /// <summary>
        /// Pops a number of items from the queue
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queue"></param>
        /// <param name="count"></param>
        public static IEnumerable<T> Dequeue<T>(this ConcurrentQueue<T> queue, int count) {
            for (var i = 0; i < count; i++) {
                if (!queue.TryDequeue(out T result)) {
                    yield break;
                }
                yield return result;
            }
        }

        /// <summary>
        /// Flattens a enumerable of enumerables
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static IEnumerable Flatten(this IEnumerable obj) {
            foreach(var item in obj) {
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
                    yield return (T)cont;
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
            foreach(var item in enumerable) {
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
