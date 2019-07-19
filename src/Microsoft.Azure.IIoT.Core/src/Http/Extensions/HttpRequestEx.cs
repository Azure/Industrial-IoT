// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http {
    using Newtonsoft.Json;
    using System.Net.Http.Headers;
    using System.Text;

    /// <summary>
    /// Http request extensions
    /// </summary>
    public static class HttpRequestEx {

        /// <summary>
        /// Set content as type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <param name="sourceObject"></param>
        /// <param name="settings"></param>
        /// <param name="encoding"></param>
        /// <param name="mediaType"></param>
        /// <returns>this</returns>
        public static IHttpRequest SetContent<T>(this IHttpRequest request, T sourceObject,
            JsonSerializerSettings settings, Encoding encoding, MediaTypeHeaderValue mediaType) {
            return request.SetContent(JsonConvert.SerializeObject(sourceObject, Formatting.None,
                settings ?? JsonConvertEx.DefaultSettings), encoding, mediaType);
        }

        /// <summary>
        /// Set content
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <param name="sourceObject"></param>
        public static IHttpRequest SetContent<T>(this IHttpRequest request, T sourceObject) {
            return request.SetContent(sourceObject, null, kDefaultEncoding, kDefaultMediaType);
        }

        /// <summary>
        /// Set content
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <param name="sourceObject"></param>
        /// <param name="settings"></param>
        public static IHttpRequest SetContent<T>(this IHttpRequest request, T sourceObject,
            JsonSerializerSettings settings) {
            return request.SetContent(sourceObject, settings, kDefaultEncoding, kDefaultMediaType);
        }

        private static readonly MediaTypeHeaderValue kDefaultMediaType =
            new MediaTypeHeaderValue(ContentEncodings.MimeTypeJson);
        private static readonly Encoding kDefaultEncoding = new UTF8Encoding();
    }
}
