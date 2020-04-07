// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub {
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Helper utility extensions of several collections and types
    /// to support simple encoding and decoding where the type
    /// itself cannot be stored as-is in the IoT Hub twin record.
    /// This includes lists and byte arrays that are longer than
    /// the max field size and more.
    /// </summary>
    public static class DeviceTwinEncodingEx {

        /// <summary>
        /// Convert list to dictionary
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static Dictionary<string, T> EncodeAsDictionary<T>(this List<T> list) {
            return EncodeAsDictionary(list, t => t);
        }

        /// <summary>
        /// Convert list to dictionary
        /// </summary>
        /// <param name="list"></param>
        /// <param name="converter"></param>
        /// <returns></returns>
        public static Dictionary<string, V> EncodeAsDictionary<T, V>(this List<T> list,
            Func<T, V> converter) {
            if (list == null) {
                return null;
            }
            var result = new Dictionary<string, V>();
            for (var i = 0; i < list.Count; i++) {
                result.Add(i.ToString(), converter(list[i]));
            }
            return result;
        }

        /// <summary>
        /// Convert dictionary to list
        /// </summary>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public static List<T> DecodeAsList<T>(this Dictionary<string, T> dictionary) {
            return DecodeAsList(dictionary, t => t);
        }

        /// <summary>
        /// Convert dictionary to list
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="converter"></param>
        /// <returns></returns>
        public static List<T> DecodeAsList<T, V>(this Dictionary<string, V> dictionary,
            Func<V, T> converter) {
            if (dictionary == null) {
                return null;
            }
            var result = Enumerable.Repeat(default(T), dictionary.Count).ToList();
            foreach (var kv in dictionary) {
                result[int.Parse(kv.Key)] = converter(kv.Value);
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

        /// <summary>
        /// Provide custom serialization by chunking a buffer
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static Dictionary<string, string> EncodeAsDictionary(this byte[] buffer) {
            if (buffer == null) {
                return null;
            }
            var str = buffer.ToBase64String() ?? string.Empty;
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
            return str.ToString().DecodeAsBase64();
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
                var add = VariantValueEx.SanitizePropertyName(s);
                if (upperCase != null) {
                    add = (bool)upperCase ? add.ToUpperInvariant() : add.ToLowerInvariant();
                }
                result.Add(add, true);
            }
            return result;
        }
    }
}
