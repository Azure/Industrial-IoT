// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.External {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public static class TwinModelEx {

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
            var str = buffer == null ? string.Empty :
                Convert.ToBase64String(buffer);
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
    }
}
