// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Newtonsoft.Json.Linq {
    using Newtonsoft.Json.Bson;
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Json token extensions
    /// </summary>
    public static class JTokenEx {

        /// <summary>
        /// hashes a json object
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static string ToSha1Hash(this JToken token) =>
            token.ToString(Formatting.None).ToSha1Hash();

        /// <summary>
        /// Creates a comparable bson buffer from token
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static byte[] ToBson(this JToken token) {
            using (var ms = new MemoryStream()) {
                using (var writer = new BsonDataWriter(ms)) {
                    if (token.Type != JTokenType.Object &&
                        token.Type != JTokenType.Array) {
                        token = new JObject {
                            new JProperty("value", token)
                        };
                    }
                    token.WriteTo(writer);
                }
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Decodes a bson buffer to a token
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static JToken FromBson(byte[] buffer) {
            if (buffer == null) {
                return JValue.CreateNull();
            }
            using (var ms = new MemoryStream(buffer)) {
                using (var reader = new BsonDataReader(ms)) {
                    return JToken.ReadFrom(reader);
                }
            }
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
        /// <returns></returns>
        public static T GetValueOrDefault<T>(this JToken t,
            string key, T defaultValue) =>
            GetValueOrDefault(t, key, () => defaultValue);

        /// <summary>
        /// Helper to get values from object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T GetValueOrDefault<T>(this JToken t,
            string key) =>
            GetValueOrDefault(t, key, () => default(T));

        /// <summary>
        /// Helper to get values from object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T GetValueOrDefault<T>(this JToken t,
            string key, Func<T> defaultValue) {
            if (t is JObject o && o.TryGetValue(key, out var token)) {
                try {
                    return token.ToObject<T>();
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
        /// <returns></returns>
        public static T? GetValueOrDefault<T>(this JToken t,
            string key, T? defaultValue) where T : struct =>
            GetValueOrDefault(t, key, () => defaultValue);

        /// <summary>
        /// Helper to get values from object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T? GetValueOrDefault<T>(this JToken t,
            string key, Func<T?> defaultValue) where T : struct {

            if (t is JObject o && o.TryGetValue(key, out var token)) {
                try {
                    return token.ToObject<T>();
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
    }
}
