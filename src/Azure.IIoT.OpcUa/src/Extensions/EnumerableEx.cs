// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Collections.Generic
{
    using System;
    using System.Linq;

    /// <summary>
    /// Enumerable helper extensions.
    /// </summary>
    public static class EnumerableEx
    {
        /// <summary>
        /// Safe sequence hash.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="seq"></param>
        /// <returns></returns>
        public static int SequenceGetHashSafe<T>(this IEnumerable<T>? seq)
        {
            return SequenceGetHashSafe(seq,
                t => EqualityComparer<T>.Default.GetHashCode(t!));
        }

        /// <summary>
        /// Safe sequence hash.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="seq"></param>
        /// <param name="hash"></param>
        /// <returns></returns>
        public static int SequenceGetHashSafe<T>(this IEnumerable<T>? seq,
            Func<T, int> hash)
        {
            var hashCode = -932366343;
            if (seq != null)
            {
                foreach (var item in seq)
                {
                    hashCode = (hashCode * -1521134295) + hash(item);
                }
            }
            return hashCode;
        }

        /// <summary>
        /// Safe sequence equals.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="seq"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool SequenceEqualsSafe<T>(this IEnumerable<T>? seq,
            IEnumerable<T>? that)
        {
            if (ReferenceEquals(seq, that))
            {
                return true;
            }
            if (seq == null || that == null)
            {
                return !(seq?.Any() ?? false) && !(that?.Any() ?? false);
            }
            return seq.SequenceEqual(that);
        }

        /// <summary>
        /// Safe sequence equals.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="seq"></param>
        /// <param name="that"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static bool SequenceEqualsSafe<T>(this IEnumerable<T>? seq,
            IEnumerable<T>? that, Func<T?, T?, bool> func)
        {
            if (ReferenceEquals(seq, that))
            {
                return true;
            }
            if (seq == null || that == null)
            {
                return !(seq?.Any() ?? false) && !(that?.Any() ?? false);
            }
            return seq.SequenceEqual(that, new FuncEqualityComparer<T>(func));
        }

        /// <summary>
        /// Safe set equals.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="seq"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool SetEqualsSafe<T>(this IEnumerable<T>? seq,
            IEnumerable<T>? that)
        {
            if (ReferenceEquals(seq, that))
            {
                return true;
            }
            if (seq == null || that == null)
            {
                return false;
            }
            if (seq is ISet<T> set)
            {
                return set.SetEquals(that);
            }
            return new HashSet<T>(seq).SetEquals(that);
        }

        /// <summary>
        /// Safe set equals.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="seq"></param>
        /// <param name="that"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static bool SetEqualsSafe<T>(this IEnumerable<T>? seq,
            IEnumerable<T>? that, Func<T?, T?, bool> func)
        {
            if (ReferenceEquals(seq, that))
            {
                return true;
            }
            if (seq == null || that == null)
            {
                return false;
            }
            var first = seq.ToList();
            var second = that.ToList();
            return first.All(x => second.Any(y => func(x, y))) &&
                second.All(y => first.Any(x => func(x, y)));
        }

        /// <summary>
        /// Execute action for each item.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="seq"></param>
        /// <param name="action"></param>
        public static void ForEach<T>(this IEnumerable<T> seq, Action<T> action)
        {
            foreach (var item in seq)
            {
                action(item);
            }
        }

        /// <summary>
        /// Add range to collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="items"></param>
        public static void AddRange<T>(this ICollection<T> collection,
            IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                collection.Add(item);
            }
        }

        /// <summary>
        /// Add or update dictionary value.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void AddOrUpdate<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            dictionary[key] = value;
        }

        private sealed class FuncEqualityComparer<T> : IEqualityComparer<T>
        {
            public FuncEqualityComparer(Func<T?, T?, bool> equals)
            {
                _equals = equals;
            }

            public bool Equals(T? x, T? y)
            {
                return _equals(x, y);
            }

            public int GetHashCode(T obj)
            {
                return obj?.GetHashCode() ?? 0;
            }

            private readonly Func<T?, T?, bool> _equals;
        }
    }

    /// <summary>
    /// Equality comparer factory.
    /// </summary>
    public static class Compare
    {
        /// <summary>
        /// Create comparer from delegate.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="equals"></param>
        /// <returns></returns>
        public static IEqualityComparer<T> Using<T>(Func<T?, T?, bool> equals)
        {
            return new FuncEqualityComparer<T>(equals);
        }

        private sealed class FuncEqualityComparer<T> : IEqualityComparer<T>
        {
            public FuncEqualityComparer(Func<T?, T?, bool> equals)
            {
                _equals = equals;
            }

            public bool Equals(T? x, T? y)
            {
                return _equals(x, y);
            }

            public int GetHashCode(T obj)
            {
                return obj?.GetHashCode() ?? 0;
            }

            private readonly Func<T?, T?, bool> _equals;
        }
    }
}
