// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http {
    using Newtonsoft.Json;
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;

    public static class HttpRequestEx {

        /// <summary>
        /// Add header value
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns>this</returns>
        public static IHttpRequest AddHeader(this IHttpRequest request, string name, 
            string value) {
            if (!request.Headers.TryAddWithoutValidation(name, value)) {
                if (name.ToLowerInvariant() != "content-type") {
                    throw new ArgumentOutOfRangeException(name, "Invalid header name");
                }
            }
            return request;
        }

        /// <summary>
        /// Set content
        /// </summary>
        /// <param name="content"></param>
        /// <param name="encoding"></param>
        /// <param name="mediaType"></param>
        /// <returns>this</returns>
        public static IHttpRequest SetContent(this IHttpRequest request, string content, 
            Encoding encoding, MediaTypeHeaderValue mediaType) {
            request.Content = new StringContent(content, encoding,
                mediaType.MediaType);
            request.Content.Headers.ContentType = mediaType;
            return request;
        }

        /// <summary>
        /// Set content as type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sourceObject"></param>
        /// <param name="encoding"></param>
        /// <param name="mediaType"></param>
        /// <returns>this</returns>
        public static IHttpRequest SetContent<T>(this IHttpRequest request, T sourceObject, 
            Encoding encoding, MediaTypeHeaderValue mediaType) => request.SetContent(
               JsonConvertEx.SerializeObject(sourceObject), encoding, mediaType);

        /// <summary>
        /// Set content
        /// </summary>
        /// <param name="request"></param>
        /// <param name="content"></param>
        public static IHttpRequest SetContent(this IHttpRequest request, string content) =>
            request.SetContent(content, kDefaultEncoding, kDefaultMediaType);

        /// <summary>
        /// Set content
        /// </summary>
        /// <param name="request"></param>
        /// <param name="content"></param>
        /// <param name="encoding"></param>
        public static IHttpRequest SetContent(this IHttpRequest request, string content,
            Encoding encoding) =>
            request.SetContent(content, encoding, kDefaultMediaType);

        /// <summary>
        /// Set content
        /// </summary>
        /// <param name="request"></param>
        /// <param name="content"></param>
        /// <param name="encoding"></param>
        /// <param name="mediaType"></param>
        public static IHttpRequest SetContent(this IHttpRequest request, string content,
            Encoding encoding, string mediaType) =>
            request.SetContent(content, encoding, new MediaTypeHeaderValue(mediaType));

        /// <summary>
        /// Set content
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <param name="sourceObject"></param>
        public static IHttpRequest SetContent<T>(this IHttpRequest request, T sourceObject) =>
            request.SetContent(sourceObject, kDefaultEncoding, kDefaultMediaType);

        /// <summary>
        /// Set content
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <param name="sourceObject"></param>
        /// <param name="encoding"></param>
        public static IHttpRequest SetContent<T>(this IHttpRequest request, T sourceObject,
            Encoding encoding) =>
            request.SetContent(sourceObject, encoding, kDefaultMediaType);

        /// <summary>
        /// Set content
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <param name="sourceObject"></param>
        /// <param name="encoding"></param>
        /// <param name="mediaType"></param>
        public static IHttpRequest SetContent<T>(this IHttpRequest request, T sourceObject,
            Encoding encoding, string mediaType) =>
            request.SetContent(sourceObject, encoding, new MediaTypeHeaderValue(mediaType));

        private static readonly MediaTypeHeaderValue kDefaultMediaType =
            new MediaTypeHeaderValue("application/json");
        private static readonly Encoding kDefaultEncoding = new UTF8Encoding();
    }
}
