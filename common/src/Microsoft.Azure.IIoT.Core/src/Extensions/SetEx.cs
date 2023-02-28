// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Collections.Generic
{
    using System.Linq;

    /// <summary>
    /// Set extensions
    /// </summary>
    public static class SetEx
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
    }
}
