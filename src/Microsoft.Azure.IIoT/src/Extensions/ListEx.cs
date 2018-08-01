// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Collections.Generic {
    using System;
    using System.Collections;

    public static class ListEx {

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
    }
}
