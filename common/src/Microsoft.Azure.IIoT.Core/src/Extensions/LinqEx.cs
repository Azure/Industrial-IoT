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
