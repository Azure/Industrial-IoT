// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http {
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;

    /// <summary>
    /// Http request extensions
    /// </summary>
    public static class HttpRequestEx {

        /// <summary>
        /// Add header value
        /// </summary>
        /// <param name="request"></param>
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
        /// <param name="request"></param>
        /// <param name="content"></param>
        /// <param name="mediaType"></param>
        /// <param name="encoding"></param>
        /// <returns>this</returns>
        public static IHttpRequest SetStringContent(this IHttpRequest request, string content,
            string mediaType = null, Encoding encoding = null) {
            return request.SetByteArrayContent((encoding ?? kDefaultEncoding).GetBytes(content),
                string.IsNullOrEmpty(mediaType) ? ContentMimeType.Json : mediaType, encoding);
        }

        /// <summary>
        /// Set content
        /// </summary>
        /// <param name="request"></param>
        /// <param name="content"></param>
        /// <param name="mediaType"></param>
        /// <param name="encoding"></param>
        /// <returns>this</returns>
        public static IHttpRequest SetByteArrayContent(this IHttpRequest request, byte[] content,
            string mediaType = null, Encoding encoding = null) {

            var headerValue = new MediaTypeHeaderValue(
                string.IsNullOrEmpty(mediaType) ? ContentMimeType.Binary : mediaType);
            if (encoding != null) {
                headerValue.CharSet = encoding.WebName;
            }
            request.Content = new ByteArrayContent(content);
            request.Content.Headers.ContentType = headerValue;
            return request;
        }

        private static readonly Encoding kDefaultEncoding = new UTF8Encoding();
    }
}
