// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.Common.Http {
    using System;
    using System.Net.Http.Headers;
    using System.Text;

    public static class HttpRequestEx {

        /// <summary>
        /// Set uri
        /// </summary>
        /// <param name="uri"></param>
        public static void SetUriFromString(this IHttpRequest request, string uri) =>
            request.Uri = new Uri(uri);

        /// <summary>
        /// Set content
        /// </summary>
        /// <param name="request"></param>
        /// <param name="content"></param>
        public static void SetContent(this IHttpRequest request, string content) =>
            request.SetContent(content, kDefaultEncoding, kDefaultMediaType);

        /// <summary>
        /// Set content
        /// </summary>
        /// <param name="request"></param>
        /// <param name="content"></param>
        /// <param name="encoding"></param>
        public static void SetContent(this IHttpRequest request, string content,
            Encoding encoding) =>
            request.SetContent(content, encoding, kDefaultMediaType);

        /// <summary>
        /// Set content
        /// </summary>
        /// <param name="request"></param>
        /// <param name="content"></param>
        /// <param name="encoding"></param>
        /// <param name="mediaType"></param>
        public static void SetContent(this IHttpRequest request, string content,
            Encoding encoding, string mediaType) =>
            request.SetContent(content, encoding, new MediaTypeHeaderValue(mediaType));

        /// <summary>
        /// Set content
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <param name="sourceObject"></param>
        public static void SetContent<T>(this IHttpRequest request, T sourceObject) =>
            request.SetContent(sourceObject, kDefaultEncoding, kDefaultMediaType);

        /// <summary>
        /// Set content
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <param name="sourceObject"></param>
        /// <param name="encoding"></param>
        public static void SetContent<T>(this IHttpRequest request, T sourceObject,
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
        public static void SetContent<T>(this IHttpRequest request, T sourceObject,
            Encoding encoding, string mediaType) =>
            request.SetContent(sourceObject, encoding, new MediaTypeHeaderValue(mediaType));

        private static readonly MediaTypeHeaderValue kDefaultMediaType =
            new MediaTypeHeaderValue("application/json");
        private static readonly Encoding kDefaultEncoding = new UTF8Encoding();
    }
}
