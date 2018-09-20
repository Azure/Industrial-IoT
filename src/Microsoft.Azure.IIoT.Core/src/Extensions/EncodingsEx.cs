// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT {
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Helper utility extensions of several types to support simple
    /// encoding and decoding where the type itself cannot be stored as is.
    /// </summary>
    public static class EncodingsEx {

        /// <summary>
        /// Convert list of strings into string
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static string EncodeAsString(this List<string> list) {
            if (list == null || !list.Any()) {
                return string.Empty;
            }
            return list.OrderBy(x => x).Aggregate((x, y) => $"{x},{y}");
        }

        /// <summary>
        /// Convert string to list
        /// </summary>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public static List<string> DecodeAsList(this string queryString) {
            if (string.IsNullOrEmpty(queryString)) {
                return null;
            }
            return queryString.Split(',').ToList();
        }

        /// <summary>
        /// Convert list to dictionary
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static Dictionary<string, T> EncodeAsDictionary<T>(this List<T> list) {
            if (list == null) {
                return null;
            }
            var result = new Dictionary<string, T>();
            for (var i = 0; i < list.Count; i++) {
                result.Add(i.ToString(), list[i]);
            }
            return result;
        }

        /// <summary>
        /// Convert dictionary to list
        /// </summary>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public static List<T> DecodeAsList<T>(this Dictionary<string, T> dictionary) {
            if (dictionary == null) {
                return null;
            }
            var result = Enumerable.Repeat(default(T), dictionary.Count).ToList();
            foreach (var kv in dictionary) {
                result[int.Parse(kv.Key)] = kv.Value;
            }
            return result;
        }

        /// <summary>
        /// Provide custom serialization by chunking a buffer
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static Dictionary<string, string> EncodeAsDictionary(this byte[] buffer) {
            if (buffer == null) {
                return null;
            }
            var str = buffer.ToBase64String() ?? string.Empty ;
            var result = new Dictionary<string, string>();
            for (var i = 0; ; i++) {
                if (str.Length < 512) {
                    result.Add($"part_{i}", str);
                    break;
                }
                var part = str.Substring(0, 512);
                result.Add($"part_{i}", part);
                str = str.Substring(512);
            }
            return result;
        }

        /// <summary>
        /// Convert chunks back to buffer
        /// </summary>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public static byte[] DecodeAsByteArray(this Dictionary<string, string> dictionary) {
            if (dictionary == null) {
                return null;
            }
            var str = new StringBuilder();
            for (var i = 0; ; i++) {
                if (!dictionary.TryGetValue($"part_{i}", out var chunk)) {
                    break;
                }
                str.Append(chunk);
            }
            if (str.Length == 0) {
                return null;
            }
            return Convert.FromBase64String(str.ToString());
        }


        /// <summary>
        /// Convert string set to queryable dictionary
        /// </summary>
        /// <param name="set"></param>
        /// <param name="upperCase"></param>
        /// <returns></returns>
        public static Dictionary<string, bool> EncodeAsDictionary(this ISet<string> set,
            bool? upperCase = null) {
            if (set == null) {
                return null;
            }
            var result = new Dictionary<string, bool>();
            foreach (var s in set) {
                var add = s.SanitizePropertyName();
                if (upperCase != null) {
                    add = (bool)upperCase ? add.ToUpperInvariant() : add.ToLowerInvariant();
                }
                result.Add(add, true);
            }
            return result;
        }

        /// <summary>
        /// Convert dictionary to string set
        /// </summary>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public static HashSet<string> DecodeAsSet(this Dictionary<string, bool> dictionary) {
            if (dictionary == null) {
                return null;
            }
            return new HashSet<string>(dictionary.Select(kv => kv.Key));
        }
    }
}
