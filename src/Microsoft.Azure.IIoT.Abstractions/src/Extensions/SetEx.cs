// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Collections.Generic {
    using System.Collections;
    using System.Linq;

    /// <summary>
    /// Set extensions
    /// </summary>
    public static class SetEx {

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
        /// <param name="set"></param>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static void AddRange(this ISet<object> set, IEnumerable enumerable) {
            foreach (var item in enumerable) {
                set.Add(item);
            }
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
    }
}
