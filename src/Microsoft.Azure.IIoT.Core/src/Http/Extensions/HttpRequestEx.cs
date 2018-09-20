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
        /// <param name="encoding"></param>
        /// <param name="mediaType"></param>
        /// <returns>this</returns>
        public static IHttpRequest SetContent<T>(this IHttpRequest request, T sourceObject,
            Encoding encoding, MediaTypeHeaderValue mediaType) => request.SetContent(
               JsonConvertEx.SerializeObject(sourceObject), encoding, mediaType);


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
            new MediaTypeHeaderValue(ContentEncodings.MimeTypeJson);
        private static readonly Encoding kDefaultEncoding = new UTF8Encoding();
    }
}
