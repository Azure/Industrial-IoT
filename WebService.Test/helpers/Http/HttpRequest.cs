// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace WebService.Test.helpers.Http
{
    public interface IHttpRequest
    {
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

    public class HttpRequest : IHttpRequest
    {
        private readonly MediaTypeHeaderValue defaultMediaType = new MediaTypeHeaderValue("application/json");
        private readonly Encoding defaultEncoding = new UTF8Encoding();

        // Http***Headers classes don't have a public ctor, so we use this class
        // to hold the headers, this is also used for PUT/POST requests body
        private readonly HttpRequestMessage requestContent = new HttpRequestMessage();

        public Uri Uri { get; set; }

        public HttpHeaders Headers => this.requestContent.Headers;

        public MediaTypeHeaderValue ContentType { get; private set; }

        public HttpRequestOptions Options { get; } = new HttpRequestOptions();

        public HttpContent Content => this.requestContent.Content;

        public HttpRequest()
        {
        }

        public HttpRequest(Uri uri)
        {
            this.Uri = uri;
        }

        public HttpRequest(string uri)
        {
            this.SetUriFromString(uri);
        }

        public void AddHeader(string name, string value)
        {
            if (!this.Headers.TryAddWithoutValidation(name, value))
            {
                if (name.ToLowerInvariant() != "content-type")
                {
                    throw new ArgumentOutOfRangeException(name, "Invalid header name");
                }

                this.ContentType = new MediaTypeHeaderValue(value);
            }
        }

        public void SetUriFromString(string uri)
        {
            this.Uri = new Uri(uri);
        }

        public void SetContent(string content)
        {
            this.SetContent(content, this.defaultEncoding, this.defaultMediaType);
        }

        public void SetContent(string content, Encoding encoding)
        {
            this.SetContent(content, encoding, this.defaultMediaType);
        }

        public void SetContent(string content, Encoding encoding, string mediaType)
        {
            this.SetContent(content, encoding, new MediaTypeHeaderValue(mediaType));
        }

        public void SetContent(string content, Encoding encoding, MediaTypeHeaderValue mediaType)
        {
            this.requestContent.Content = new StringContent(content, encoding, mediaType.MediaType);
            this.ContentType = mediaType;
        }

        public void SetContent(StringContent stringContent)
        {
            this.requestContent.Content = stringContent;
            this.ContentType = stringContent.Headers.ContentType;
        }

        public void SetContent<T>(T sourceObject)
        {
            this.SetContent(sourceObject, this.defaultEncoding, this.defaultMediaType);
        }

        public void SetContent<T>(T sourceObject, Encoding encoding)
        {
            this.SetContent(sourceObject, encoding, this.defaultMediaType);
        }

        public void SetContent<T>(T sourceObject, Encoding encoding, string mediaType)
        {
            this.SetContent(sourceObject, encoding, new MediaTypeHeaderValue(mediaType));
        }

        public void SetContent<T>(T sourceObject, Encoding encoding, MediaTypeHeaderValue mediaType)
        {
            var content = JsonConvert.SerializeObject(sourceObject, Formatting.None);
            this.requestContent.Content = new StringContent(content, encoding, mediaType.MediaType);
            this.ContentType = mediaType;
        }
    }
}
