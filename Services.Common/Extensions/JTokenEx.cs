// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Newtonsoft.Json.Linq {
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    public static class JTokenEx {

        /// <summary>
        /// Minify json by removing all whitespace, see
        /// https://stackoverflow.com/questions/8913138/minify-indented-json-string-in-net
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static string ToStringMinified(this JToken token) {
            return Regex.Replace(token.ToString(),
                "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1");
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
            return token.ToStringMinified().Equals(json, comparison);
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
            return token.ToStringMinified().Equals(other.ToStringMinified(), comparison);
        }

        /// <summary>
        /// Helper to get values from token dictionary
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T Get<T>(this Dictionary<string, JToken> dict,
            string key, T defaultValue) {
            if (dict.TryGetValue(key, out var token)) {
                return token.ToObject<T>();
            }
            return defaultValue;
        }

        /// <summary>
        /// Helper to get values from object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="o"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T Get<T>(this JObject o,
            string key, T defaultValue) {
            if (o.TryGetValue(key, out var token)) {
                return token.ToObject<T>();
            }
            return defaultValue;
        }
    }
}
