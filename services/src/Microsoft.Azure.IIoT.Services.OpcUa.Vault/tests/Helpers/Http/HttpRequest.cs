// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.Tests.Helpers.Http {
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using Newtonsoft.Json;

    public interface IHttpRequest {
        Uri Uri { get; set; }

        HttpHeaders Headers { get; }

        MediaTypeHeaderValue ContentType { get; }

        HttpRequestOptions Options { get; }

        HttpContent Content { get; }

        void AddHeader(string name, string value);

        void SetUriFromString(string uri);

        void SetContent(string content);

        void SetContent(string content, Encoding encoding);

        void SetContent(string content, Encoding encoding, string mediaType);

        void SetContent(string content, Encoding encoding, MediaTypeHeaderValue mediaType);

        void SetContent(StringContent stringContent);

        void SetContent<T>(T sourceObject);

        void SetContent<T>(T sourceObject, Encoding encoding);

        void SetContent<T>(T sourceObject, Encoding encoding, string mediaType);

        void SetContent<T>(T sourceObject, Encoding encoding, MediaTypeHeaderValue mediaType);
    }

    public class HttpRequest : IHttpRequest {
        private readonly MediaTypeHeaderValue _defaultMediaType = new MediaTypeHeaderValue("application/json");
        private readonly Encoding _defaultEncoding = new UTF8Encoding();

        // Http***Headers classes don't have a public ctor, so we use this class
        // to hold the headers, this is also used for PUT/POST requests body
        private readonly HttpRequestMessage _requestContent = new HttpRequestMessage();

        public Uri Uri { get; set; }

        public HttpHeaders Headers => _requestContent.Headers;

        public MediaTypeHeaderValue ContentType { get; private set; }

        public HttpRequestOptions Options { get; } = new HttpRequestOptions();

        public HttpContent Content => _requestContent.Content;

        public HttpRequest() {
        }

        public HttpRequest(Uri uri) {
            Uri = uri;
        }

        public HttpRequest(string uri) {
            SetUriFromString(uri);
        }

        public void AddHeader(string name, string value) {
            if (!Headers.TryAddWithoutValidation(name, value)) {
                if (name.ToLowerInvariant() != "content-type") {
                    throw new ArgumentOutOfRangeException(name, "Invalid header name");
                }

                ContentType = new MediaTypeHeaderValue(value);
            }
        }

        public void SetUriFromString(string uri) {
            Uri = new Uri(uri);
        }

        public void SetContent(string content) {
            SetContent(content, _defaultEncoding, _defaultMediaType);
        }

        public void SetContent(string content, Encoding encoding) {
            SetContent(content, encoding, _defaultMediaType);
        }

        public void SetContent(string content, Encoding encoding, string mediaType) {
            SetContent(content, encoding, new MediaTypeHeaderValue(mediaType));
        }

        public void SetContent(string content, Encoding encoding, MediaTypeHeaderValue mediaType) {
            _requestContent.Content = new StringContent(content, encoding, mediaType.MediaType);
            ContentType = mediaType;
        }

        public void SetContent(StringContent stringContent) {
            _requestContent.Content = stringContent;
            ContentType = stringContent.Headers.ContentType;
        }

        public void SetContent<T>(T sourceObject) {
            SetContent(sourceObject, _defaultEncoding, _defaultMediaType);
        }

        public void SetContent<T>(T sourceObject, Encoding encoding) {
            SetContent(sourceObject, encoding, _defaultMediaType);
        }

        public void SetContent<T>(T sourceObject, Encoding encoding, string mediaType) {
            SetContent(sourceObject, encoding, new MediaTypeHeaderValue(mediaType));
        }

        public void SetContent<T>(T sourceObject, Encoding encoding, MediaTypeHeaderValue mediaType) {
            var content = JsonConvertEx.SerializeObject(sourceObject);
            _requestContent.Content = new StringContent(content, encoding, mediaType.MediaType);
            ContentType = mediaType;
        }
    }
}
