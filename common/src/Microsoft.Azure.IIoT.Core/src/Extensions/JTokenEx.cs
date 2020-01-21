// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Newtonsoft.Json.Linq {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Json token extensions
    /// </summary>
    public static class JTokenEx {

        /// <summary>
        /// hashes a json object
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static string ToSha1Hash(this JToken token) {
            return token.ToString(Formatting.None).ToSha1Hash();
        }

        /// <summary>
        /// Helper to get values from token dictionary
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T GetValueOrDefault<T>(this Dictionary<string, JToken> dict,
            string key, T defaultValue) {
            if (dict != null && dict.TryGetValue(key, out var token)) {
                try {
                    return token.ToObject<T>();
                }
                catch {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Helper to get values from token dictionary
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T? GetValueOrDefault<T>(this Dictionary<string, JToken> dict,
            string key, T? defaultValue) where T : struct {
            if (dict != null && dict.TryGetValue(key, out var token)) {
                try {
                    return token.ToObject<T>();
                }
                catch {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Helper to get values from object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <param name="compare"></param>
        /// <returns></returns>
        public static T GetValueOrDefault<T>(this JToken t,
            string key, T defaultValue,
            StringComparison compare = StringComparison.Ordinal) {
            return GetValueOrDefault(t, key, () => defaultValue, compare);
        }

        /// <summary>
        /// Helper to get values from object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="key"></param>
        /// <param name="compare"></param>
        /// <returns></returns>
        public static T GetValueOrDefault<T>(this JToken t,
            string key, StringComparison compare = StringComparison.Ordinal) {
            return GetValueOrDefault(t, key, () => default(T), compare);
        }

        /// <summary>
        /// Helper to get values from object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <param name="compare"></param>
        /// <returns></returns>
        public static T GetValueOrDefault<T>(this JToken t,
            string key, Func<T> defaultValue,
            StringComparison compare = StringComparison.Ordinal) {
            if (t is JObject o) {
                try {
                    var value = o.Property(key, compare)?.Value;
                    if (value != null) {
                        return value.ToObject<T>();
                    }
                }
                catch {
                    return defaultValue();
                }
            }
            return defaultValue();
        }

        /// <summary>
        /// Helper to get values from object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <param name="compare"></param>
        /// <returns></returns>
        public static T? GetValueOrDefault<T>(this JToken t,
            string key, T? defaultValue,
            StringComparison compare = StringComparison.Ordinal) where T : struct {
            return GetValueOrDefault(t, key, () => defaultValue, compare);
        }

        /// <summary>
        /// Helper to get values from object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <param name="compare"></param>
        /// <returns></returns>
        public static T? GetValueOrDefault<T>(this JToken t,
            string key, Func<T?> defaultValue,
            StringComparison compare = StringComparison.Ordinal) where T : struct {

            if (t is JObject o) {
                try {
                    var value = o.Property(key, compare)?.Value;
                    if (value != null) {
                        return value.ToObject<T>();
                    }
                }
                catch {
                    return defaultValue();
                }
            }
            return defaultValue();
        }

        /// <summary>
        /// Replace whitespace in a property name
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SanitizePropertyName(string value) {
            var chars = new char[value.Length];
            for (var i = 0; i < value.Length; i++) {
                chars[i] = !char.IsLetterOrDigit(value[i]) ? '_' : value[i];
            }
            return new string(chars);
        }

        /// <summary>
        /// Returns dimensions of the multi dimensional array assuming
        /// it is not jagged.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static int[] GetDimensions(this JArray array, out JTokenType type) {
            var dimensions = new List<int>();
            type = JTokenType.Undefined;
            while (true) {
                if (array == null || array.Count == 0) {
                    break;
                }
                dimensions.Add(array.Count);
                type = array[0].Type;
                array = array[0] as JArray;
            }
            return dimensions.ToArray();
        }

        /// <summary>
        /// Returns whether the token is a float type
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static bool IsFloatValue(this JToken token) {
            if (token?.Type == JTokenType.Float) {
                return true;
            }
            if (token?.Type == JTokenType.String) {
                var val = (string)token;
                if (val == "NaN" || val == "Infinity" || val == "-Infinity") {
                    return true;
                }
            }
            return false;
        }
    }
}
