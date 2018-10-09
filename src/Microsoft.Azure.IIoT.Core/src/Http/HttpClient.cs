// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http.Default {
    using Microsoft.Azure.IIoT.Diagnostics;
    using System;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using System.Net;

    /// <summary>
    /// Http client wrapping http client factory created http clients and
    /// abstracting away all the http client factory and handler noise
    /// for easy injection.
    /// </summary>
    public class HttpClient : IHttpClient {

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="logger"></param>
        public HttpClient(ILogger logger) :
            this(null, logger) {
        }

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="factory"></param>
        public HttpClient(IHttpClientFactory factory, ILogger logger) {
            _factory = factory ?? new HttpClientFactory(null, logger);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        /// <inheritdoc/>
        public IHttpRequest NewRequest(Uri uri, string resourceId) =>
            new HttpRequest(uri, resourceId);

        /// <inheritdoc/>
        public Task<IHttpResponse> GetAsync(IHttpRequest request) =>
            SendAsync(request, HttpMethod.Get);

        /// <inheritdoc/>
        public Task<IHttpResponse> PostAsync(IHttpRequest request) =>
            SendAsync(request, HttpMethod.Post);

        /// <inheritdoc/>
        public Task<IHttpResponse> PutAsync(IHttpRequest request) =>
            SendAsync(request, HttpMethod.Put);

        /// <inheritdoc/>
        public Task<IHttpResponse> PatchAsync(IHttpRequest request) =>
            SendAsync(request, new HttpMethod("PATCH"));

        /// <inheritdoc/>
        public Task<IHttpResponse> DeleteAsync(IHttpRequest request) =>
            SendAsync(request, HttpMethod.Delete);

        /// <inheritdoc/>
        public Task<IHttpResponse> HeadAsync(IHttpRequest request) =>
            SendAsync(request, HttpMethod.Head);

        /// <inheritdoc/>
        public Task<IHttpResponse> OptionsAsync(IHttpRequest request) =>
            SendAsync(request, HttpMethod.Options);

        /// <summary>
        /// Send request
        /// </summary>
        /// <param name="httpRequest"></param>
        /// <param name="httpMethod"></param>
        /// <returns></returns>
        private async Task<IHttpResponse> SendAsync(IHttpRequest httpRequest,
            HttpMethod httpMethod) {

            if (!(httpRequest is HttpRequest wrapper)) {
                throw new InvalidOperationException("Bad request");
            }
#if DEBUG
            // Increase timeout for the case where debugger is attached
            if (Debugger.IsAttached) {
                httpRequest.Options.Timeout *= 100;
            }
#endif
            using (var client = _factory.CreateClient(httpRequest.ResourceId ??
                HttpHandlerFactory.kDefaultResourceId)) {
                client.Timeout = TimeSpan.FromMilliseconds(httpRequest.Options.Timeout);
                var sw = Stopwatch.StartNew();
                _logger.Verbose($"Sending {httpMethod} request to {httpRequest.Uri}...");
                try {
                    wrapper.Request.Method = httpMethod;
                    using (var response = await client.SendAsync(wrapper.Request)) {
                        var result = new HttpResponse {
                            ResourceId = httpRequest.ResourceId,
                            StatusCode = response.StatusCode,
                            Headers = response.Headers,
                            Content = await response.Content.ReadAsByteArrayAsync()
                        };
                        if (result.IsError()) {
                            _logger.Error($"{httpMethod} to {httpRequest.Uri} returned " +
                                 $"{response.StatusCode} (took {sw.Elapsed}).",
                                 () => result.GetContentAsString());
                        }
                        else {
                            _logger.Verbose($"... {httpMethod} to {httpRequest.Uri} returned " +
                                 $"{response.StatusCode} (took {sw.Elapsed}).");
                        }
                        return result;
                    }
                }
                catch (HttpRequestException e) {
                    var errorMessage = e.Message;
                    if (e.InnerException != null) {
                        errorMessage += " - " + e.InnerException.Message;
                    }
                    _logger.Error(
                        $"... {httpMethod} to {httpRequest.Uri} failed (after {sw.Elapsed})!",
                        () => new {
                            ExceptionMessage = e.Message,
                            InnerExceptionType = e.InnerException?.GetType().FullName ?? "",
                            InnerExceptionMessage = e.InnerException?.Message ?? "",
                            errorMessage
                        });
                    throw new HttpRequestException(errorMessage, e);
                }
            }
        }

        /// <summary>
        /// Request object
        /// </summary>
        public class HttpRequest : IHttpRequest {

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="uri"></param>
            /// <param name="resourceId"></param>
            public HttpRequest(Uri uri, string resourceId) {
                Options = new HttpRequestOptions();
                Request = new HttpRequestMessage {
                    RequestUri = uri
                };
                ResourceId = resourceId;
                if (ResourceId != null) {
                    Request.Headers.TryAddWithoutValidation(
                        HttpHeader.ResourceId, ResourceId);
                }
            }

            /// <summary>
            /// The request
            /// </summary>
            public HttpRequestMessage Request { get; }

            /// <inheritdoc/>
            public Uri Uri => Request.RequestUri;

            /// <inheritdoc/>
            public HttpRequestHeaders Headers => Request.Headers;

            /// <inheritdoc/>
            public HttpRequestOptions Options { get; }

            /// <inheritdoc/>
            public HttpContent Content {
                get => Request.Content;
                set => Request.Content = value;
            }

            /// <inheritdoc/>
            public string ResourceId { get; }
        }

        /// <summary>
        /// Response object
        /// </summary>
        public class HttpResponse : IHttpResponse {

            /// <inheritdoc/>
            public string ResourceId { get; internal set; }

            /// <inheritdoc/>
            public HttpStatusCode StatusCode { get; internal set; }

            /// <inheritdoc/>
            public HttpResponseHeaders Headers { get; internal set; }

            /// <inheritdoc/>
            public byte[] Content { get; internal set; }
        }

        private readonly IHttpClientFactory _factory;
        private readonly ILogger _logger;
    }
}
