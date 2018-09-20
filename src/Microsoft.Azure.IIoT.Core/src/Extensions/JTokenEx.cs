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
        /// Compare two json snippets for same content
        /// </summary>
        /// <param name="token"></param>
        /// <param name="json"></param>
        /// <param name="comparison"></param>
        /// <returns></returns>
        public static bool SameAs(this JToken token, string json,
            StringComparison comparison) {
            if (token is null || json is null) {
                return false;
            }
            return token.ToString(Formatting.None).Equals(json, comparison);
        }

        /// <summary>
        /// string compare two tokens
        /// </summary>
        /// <param name="token"></param>
        /// <param name="other"></param>
        /// <param name="comparison"></param>
        /// <returns></returns>
        public static bool SameAs(this JToken token, JToken other,
            StringComparison comparison) {
            if (ReferenceEquals(token, other)) {
                return true;
            }
            if (token is null || other is null) {
                return false;
            }
            return token.ToString(Formatting.None).Equals(
                other.ToString(Formatting.None), comparison);
        }

        /// <summary>
        /// Helper to get values from token dictionary
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T GetValue<T>(this Dictionary<string, JToken> dict,
            string key, T defaultValue) {
            if (dict.TryGetValue(key, out var token)) {
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
        public static T? GetValue<T>(this Dictionary<string, JToken> dict,
            string key, T? defaultValue) where T : struct {
            if (dict.TryGetValue(key, out var token)) {
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
        public static T GetValue<T>(this JToken t,
            string key, T defaultValue) {
            if (t is JObject o && o.TryGetValue(key, out var token)) {
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
        public static T? GetValue<T>(this JToken t,
            string key, T? defaultValue) where T : struct {

            if (t is JObject o && o.TryGetValue(key, out var token)) {
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
        /// Replace whitespace in a property name
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SanitizePropertyName(this string value) {
            var chars = new char[checked(value.Length)];
            unchecked {
                for (var i = 0; i < value.Length; i++) {
                    chars[i] = !char.IsLetterOrDigit(value[i]) ? '_' : value[i];
                }
            }
            return new string(chars);
        }

        /// <summary>
        /// Apply a patch to the token
        /// </summary>
        /// <returns></returns>
        public static JToken ApplyPatch(this JToken target,
            JToken patch) {
            if (patch == null) {
                return JValue.CreateNull();
            }

            //
            // If different types, go for the patch token
            //
            if (target == null || target.Type != patch.Type) {
                return patch;
            }

            //
            // Object is patched by removing all items that have
            // a null in the patch, and adding items that are
            // different
            //
            if (target is JObject o) {
                foreach (var prop in (JObject)patch) {
                    if (o.TryGetValue(prop.Key,
                        out var existing)) {
                        o.Remove(prop.Key);
                    }
                    var p = ApplyPatch(existing, prop.Value);
                    if (p.Type != JTokenType.Null) {
                        o.Add(prop.Key, p);
                    }
                }
                return o;
            }

            //
            // Array is patched by removing all items with null at
            // a particular index in the original array and filling
            // up the remainder with data from either array.
            //
            if (target is JArray a) {
                var f = (JArray)patch;
                var n = new JArray();
                for (var i = 0;
                    i < Math.Max(a.Count, f.Count); i++) {
                    if (i >= f.Count) {
                        n.Add(a[i]);
                        continue;
                    }
                    var p = (i >= a.Count) ? f[i] :
                        a[i].ApplyPatch(f[i]);
                    if (p.Type != JTokenType.Null) {
                        n.Add(p);
                    }
                }
                return n;
            }

            //
            // Replace anything else...
            //
            return patch;
        }
    }
}
