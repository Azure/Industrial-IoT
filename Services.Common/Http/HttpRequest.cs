// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.Common.Http {
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using Newtonsoft.Json;

    /// <summary>
    /// Request object
    /// </summary>
    public class HttpRequest : IHttpRequest {

        /// <summary>
        /// Uri of the request
        /// </summary>
        public Uri Uri { get; set; }

        /// <summary>
        /// Headers
        /// </summary>
        public HttpHeaders Headers => _requestContent.Headers;

        /// <summary>
        /// Content type
        /// </summary>
        public MediaTypeHeaderValue ContentType { get; private set; }

        /// <summary>
        /// Request options
        /// </summary>
        public HttpRequestOptions Options { get; } = new HttpRequestOptions();

        /// <summary>
        /// Content
        /// </summary>
        public HttpContent Content => _requestContent.Content;

        /// <summary>
        /// Create request
        /// </summary>
        public HttpRequest() {
            _requestContent = new HttpRequestMessage();
        }

        /// <summary>
        /// Add header value
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void AddHeader(string name, string value) {
            if (!Headers.TryAddWithoutValidation(name, value)) {
                if (name.ToLowerInvariant() != "content-type") {
                    throw new ArgumentOutOfRangeException(name, "Invalid header name");
                }
                ContentType = new MediaTypeHeaderValue(value);
            }
        }

        /// <summary>
        /// Set content
        /// </summary>
        /// <param name="content"></param>
        /// <param name="encoding"></param>
        /// <param name="mediaType"></param>
        public void SetContent(string content, Encoding encoding,
            MediaTypeHeaderValue mediaType) {
            _requestContent.Content = new StringContent(content, encoding,
                mediaType.MediaType);
            ContentType = mediaType;
        }

        /// <summary>
        /// Set content as type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sourceObject"></param>
        /// <param name="encoding"></param>
        /// <param name="mediaType"></param>
        public void SetContent<T>(T sourceObject, Encoding encoding,
            MediaTypeHeaderValue mediaType) {
            var content = JsonConvertEx.SerializeObject(sourceObject);
            _requestContent.Content = new StringContent(content, encoding,
                mediaType.MediaType);
            ContentType = mediaType;
        }

        private readonly HttpRequestMessage _requestContent;
    }
}
