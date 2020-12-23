// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Collections.Generic {

    /// <summary>
    /// List extensions
    /// </summary>
    public static class ListEx {

        private static readonly Random rng = new Random();

        /// <summary>
        /// Shuffle list
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static IList<T> Shuffle<T>(this IList<T> list) {
            if (list == null) {
                throw new ArgumentNullException(nameof(list));
            }
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
        /// Add range
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="range"></param>
        public static void AddRange<T>(this IList<T> list, IEnumerable<T> range) {
            if (list == null) {
                throw new ArgumentNullException(nameof(list));
            }
            if (range == null) {
                return;
            }
            foreach (var item in range) {
                list.Add(item);
            }
        }
    }
}
