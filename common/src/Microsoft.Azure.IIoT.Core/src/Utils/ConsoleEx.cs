// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System {
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Console extensions
    /// </summary>
    public static class ConsoleEx {

        /// <summary>
        /// Select from an array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="toString"></param>
        /// <param name="exit"></param>
        /// <returns></returns>
        public static T Select<T>(IEnumerable<T> items, Func<T, string> toString,
            string exit = "") {
            var arr = items.ToArray();
            if (arr.Length == 0) {
                return default;
            }
            for (var i = 0; i < arr.Length; i++) {
                Console.WriteLine($"[{i}] {toString(arr[i])}");
            }
            while (true) {
                Console.Write("> ");
                var line = Console.ReadLine();
                if (line == exit) {
                    return default;
                }
                if (!int.TryParse(line, out var selected)) {
                    Console.WriteLine(line + " is not a valid index!");
                    continue;
                }
                if (selected < 0 || selected >= arr.Length) {
                    Console.WriteLine($"Select from index 0 - " +
                        arr.Length);
                    continue;
                }
                return arr[selected];
            }
        }

        /// <summary>
        /// Select from an array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="exit"></param>
        /// <returns></returns>
        public static T Select<T>(IEnumerable<T> items, string exit = "") {
            return Select(items, t => t.ToString(), exit);
        }
    }
}
