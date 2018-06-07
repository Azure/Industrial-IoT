// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http.Default {
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Diagnostics;
    using System;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using System.Net;

    /// <summary>
    /// Client implementation
    /// </summary>
    public class HttpClient : IHttpClient {

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="logger"></param>
        public HttpClient(ILogger logger, IHttpAuthHandler auth) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));
        }

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="logger"></param>
        public HttpClient(ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.Info("Requests are not authenticated", () => { });
        }

        /// <summary>
        /// Create new request
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public IHttpRequest NewRequest(Uri uri, string resourceId) =>
            new HttpRequest(uri, resourceId);

        /// <summary>
        /// Perform get
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<IHttpResponse> GetAsync(IHttpRequest request) =>
            SendAsync(request, HttpMethod.Get);

        /// <summary>
        /// Perform post
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<IHttpResponse> PostAsync(IHttpRequest request) =>
            SendAsync(request, HttpMethod.Post);

        /// <summary>
        /// Perform put
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<IHttpResponse> PutAsync(IHttpRequest request) =>
            SendAsync(request, HttpMethod.Put);

        /// <summary>
        /// Performs patch
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<IHttpResponse> PatchAsync(IHttpRequest request) =>
            SendAsync(request, new HttpMethod("PATCH"));

        /// <summary>
        /// Performs delete
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<IHttpResponse> DeleteAsync(IHttpRequest request) =>
            SendAsync(request, HttpMethod.Delete);

        /// <summary>
        /// Performs head
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<IHttpResponse> HeadAsync(IHttpRequest request) =>
            SendAsync(request, HttpMethod.Head);

        /// <summary>
        /// Performs option
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
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

            if (!(httpRequest is HttpRequest build)) {
                throw new InvalidOperationException("Bad request");
            }

            if (_auth != null && httpRequest.ResourceId != null) {
                await _auth.OnRequestAsync(httpRequest);
            }

            var request = build.Request;
            request.Method = httpMethod;

            _logger.Debug($"Sending {httpMethod} request to {httpRequest.Uri}...",
                () => { });
            var sw = Stopwatch.StartNew();
            var handler = new HttpClientHandler();
#if DEBUG
            httpRequest.Options.Timeout *= 100;
            handler.ServerCertificateCustomValidationCallback = (a, b, c, d) => true;
#endif
            using (var client = new System.Net.Http.HttpClient(handler)) {
                client.Timeout = TimeSpan.FromMilliseconds(httpRequest.Options.Timeout);
                try {
                    using (var response = await client.SendAsync(request)) {
                        _logger.Debug($"... {httpMethod} to {httpRequest.Uri} returned " +
                            $"{response.StatusCode} (took {sw.Elapsed}).", () => {});

                        var httpResponse = new HttpResponse {
                            ResourceId = httpRequest.ResourceId,
                            StatusCode = response.StatusCode,
                            Headers = response.Headers,
                            Content = await response.Content.ReadAsStringAsync()
                        };
                        if (_auth != null && httpRequest.ResourceId != null) {
                            // Invalidate token cache for resource
                            await _auth.OnResponseAsync(httpResponse);
                        }
                        return httpResponse;
                    }
                }
                catch (HttpRequestException e) {
                    var errorMessage = e.Message;
                    if (e.InnerException != null) {
                        errorMessage += " - " + e.InnerException.Message;
                    }
                    _logger.Error(
                        $"... {httpMethod} to {httpRequest.Uri} failed (took {sw.Elapsed})!",
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
            public string Content { get; internal set; }
        }

        private readonly ILogger _logger;
        private readonly IHttpAuthHandler _auth;
    }
}
