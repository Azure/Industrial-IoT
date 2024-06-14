// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Linq
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

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
    }
}
