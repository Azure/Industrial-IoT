// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Models {
    using Microsoft.Azure.IIoT.Http;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Callback model extensions
    /// </summary>
    public static class CallbackModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static CallbackModel Clone(this CallbackModel model) {
            if (model == null) {
                return null;
            }
            return new CallbackModel {
                AuthenticationHeader = model.AuthenticationHeader,
                Method = model.Method,
                Uri = model.Uri
            };
        }

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this CallbackModel model, CallbackModel that) {
            if (model == that) {
                return true;
            }
            if (model == null || that == null) {
                return false;
            }
            return
                that.Uri == model.Uri &&
                that.AuthenticationHeader == model.AuthenticationHeader &&
                (that.Method ?? CallbackMethodType.Get) ==
                    (model.Method ?? CallbackMethodType.Get);
        }

        /// <summary>
        /// Call all callbacks
        /// </summary>
        /// <param name="client"></param>
        /// <param name="models"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        public static Task CallAsync(this IHttpClient client, JToken payload,
            params CallbackModel[] models) {
            if (models == null || models.Length == 0) {
                return Task.CompletedTask;
            }
            var query = Append("", payload);
            return Task.WhenAll(models.Select(m => CallAsync(client, payload, query, m)));
        }

        /// <summary>
        /// Internal callback helper
        /// </summary>
        /// <param name="client"></param>
        /// <param name="payload"></param>
        /// <param name="query"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        internal static Task CallAsync(IHttpClient client, JToken payload,
            string query, CallbackModel model) {
            var builder = new UriBuilder(model.Uri);
            if (!string.IsNullOrEmpty(query)) {
                if (string.IsNullOrEmpty(builder.Query)) {
                    builder.Query = "?";
                }
                else {
                    builder.Query += "&";
                }
                builder.Query += query;
            }
            var request = client.NewRequest(builder.Uri);
            if (!string.IsNullOrWhiteSpace(model.AuthenticationHeader)) {
                request.AddHeader("Authentication", model.AuthenticationHeader);
            }
            switch (model.Method) {
                case CallbackMethodType.Put:
                    if (payload != null) {
                        request.SetContent(payload);
                    }
                    return client.PutAsync(request);
                case CallbackMethodType.Delete:
                    return client.DeleteAsync(request);
                case CallbackMethodType.Get:
                    return client.GetAsync(request);
                case CallbackMethodType.Post:
                    if (payload != null) {
                        request.SetContent(payload);
                    }
                    return client.PostAsync(request);
                default:
                    return Task.FromException(new ArgumentException("bad method value",
                        nameof(model.Method)));
            }
        }

        /// <summary>
        /// Append payload to query
        /// </summary>
        /// <param name="query"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private static string Append(string query, JToken token) {
            if (token == null || token.Type == JTokenType.Null) {
                return null;
            }
            switch (token) {
                case JObject o:
                    Append(query, o);
                    break;
                case JValue v:
                    query = Append(query, "param", v);
                    break;
                case JArray a:
                    query = Append(query, "param", a);
                    break;
            }
            return query;
        }

        /// <summary>
        /// Append object to query
        /// </summary>
        /// <param name="query"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        private static string Append(string query, JObject obj) {
            foreach (var p in obj.Children<JProperty>()) {
                switch (p.Value) {
                    case JValue v:
                        query = Append(query, p.Name, v);
                        break;
                    case JArray a:
                        query = Append(query, p.Name, a);
                        break;
                }
            }
            return query;
        }

        /// <summary>
        /// Append value to query
        /// </summary>
        /// <param name="query"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string Append(string query, string name,
            JValue value) {
            if (value != null && value.Type != JTokenType.Null) {
                if (!string.IsNullOrEmpty(query)) {
                    query += "&";
                }
                query += name.UrlEncode() + "=" + value.ToString().UrlEncode();
            }
            return query;
        }

        /// <summary>
        /// Append array to query
        /// </summary>
        /// <param name="query"></param>
        /// <param name="name"></param>
        /// <param name="array"></param>
        /// <returns></returns>
        private static string Append(string query, string name,
            JArray array) {
            if (array != null && array.Type != JTokenType.Null) {
                if (!string.IsNullOrEmpty(query)) {
                    query += "&";
                }
                foreach (var item in array) {
                    if (item is JValue v) {
                        query = Append(query, name, v);
                    }
                }
            }
            return query;
        }
    }
}
