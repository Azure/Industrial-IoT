// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Linq
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Enumerable extensions
    /// </summary>
    public static class LinqEx2
    {
        /// <summary>
        /// Create batches of enumerables.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="count"></param>
        /// <exception cref="ArgumentException"></exception>
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> items,
            int count)
        {
            if (count <= 0)
            {
                throw new ArgumentException("Cannot create 0 or negative size batches");
            }
            return items
                .Select((x, i) => Tuple.Create(x, i))
                .GroupBy(x => x.Item2 / count)
                .Select(g => g.Select(x => x.Item1));
        }

        /// <summary>
        /// Merge enumerable b into set a.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(a))]
        public static IReadOnlySet<T>? MergeWith<T>(this IReadOnlySet<T>? a, IEnumerable<T>? b)
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
        /// Creates a hash set from enumerable or null if enumerable is null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(enumerable))]
        public static HashSet<T>? ToHashSetSafe<T>(this IEnumerable<T>? enumerable)
        {
            if (enumerable == null)
            {
                return null;
            }
            return new HashSet<T>(enumerable);
        }

        /// <summary>
        /// Return object as an enumerable with one element.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IEnumerable<T> YieldReturn<T>(this T value)
        {
            yield return value;
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
                    //
                    // Continue rather than break. Breaking here abandoned every
                    // element after the first nested one, so flattening
                    // [[1,[2,3]], 4] silently lost the 4.
                    //
                    contained = contained.Flatten();
                    foreach (var cont in contained)
                    {
                        yield return cont;
                    }
                    continue;
                }
                yield return item;
            }
        }
    }
}
