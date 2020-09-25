// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http.Default {
    using Serilog;
    using System;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using System.Net;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Http client wrapping http client factory created http clients and
    /// abstracting away all the http client factory and handler noise
    /// for easy injection.
    /// </summary>
    public sealed class HttpClient : IHttpClient {

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
        /// <param name="factory"></param>
        /// <param name="logger"></param>
        public HttpClient(IHttpClientFactory factory, ILogger logger) {
            _factory = factory ?? new HttpClientFactory(null, logger);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        /// <inheritdoc/>
        public IHttpRequest NewRequest(Uri uri, string resourceId) {
            return new HttpRequest(uri, resourceId);
        }

        /// <inheritdoc/>
        public Task<IHttpResponse> GetAsync(IHttpRequest request, CancellationToken ct) {
            return SendAsync(request, HttpMethod.Get, ct);
        }

        /// <inheritdoc/>
        public Task<IHttpResponse> PostAsync(IHttpRequest request, CancellationToken ct) {
            return SendAsync(request, HttpMethod.Post, ct);
        }

        /// <inheritdoc/>
        public Task<IHttpResponse> PutAsync(IHttpRequest request, CancellationToken ct) {
            return SendAsync(request, HttpMethod.Put, ct);
        }

        /// <inheritdoc/>
        public Task<IHttpResponse> PatchAsync(IHttpRequest request, CancellationToken ct) {
            return SendAsync(request, new HttpMethod("PATCH"), ct);
        }

        /// <inheritdoc/>
        public Task<IHttpResponse> DeleteAsync(IHttpRequest request, CancellationToken ct) {
            return SendAsync(request, HttpMethod.Delete, ct);
        }

        /// <inheritdoc/>
        public Task<IHttpResponse> HeadAsync(IHttpRequest request, CancellationToken ct) {
            return SendAsync(request, HttpMethod.Head, ct);
        }

        /// <inheritdoc/>
        public Task<IHttpResponse> OptionsAsync(IHttpRequest request, CancellationToken ct) {
            return SendAsync(request, HttpMethod.Options, ct);
        }

        /// <summary>
        /// Send request
        /// </summary>
        /// <param name="httpRequest"></param>
        /// <param name="httpMethod"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<IHttpResponse> SendAsync(IHttpRequest httpRequest,
            HttpMethod httpMethod, CancellationToken ct) {

            if (!(httpRequest is HttpRequest wrapper)) {
                throw new InvalidOperationException("Bad request");
            }

            using (var client = _factory.CreateClient(httpRequest.ResourceId ??
                HttpHandlerFactory.DefaultResourceId)) {

                if (httpRequest.Options.Timeout.HasValue) {
                    client.Timeout = httpRequest.Options.Timeout.Value;
                }

                var sw = Stopwatch.StartNew();
                _logger.Verbose("Sending {method} request to {uri}...", httpMethod,
                    httpRequest.Uri);

                // We will use this local function for Exception formatting
                HttpRequestException generateHttpRequestException(Exception e) {
                    var errorMessage = e.Message;
                    if (e.InnerException != null) {
                        errorMessage += " - " + e.InnerException.Message;
                    }
                    _logger.Warning("{method} to {uri} failed (after {elapsed}) : {message}!",
                        httpMethod, httpRequest.Uri, sw.Elapsed, errorMessage);
                    _logger.Verbose(e, "{method} to {uri} failed (after {elapsed}) : {message}!",
                        httpMethod, httpRequest.Uri, sw.Elapsed, errorMessage);
                    return new HttpRequestException(errorMessage, e);
                }

                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct)) {
                    try {
                        wrapper.Request.Method = httpMethod;
                        using (var response = await client.SendAsync(wrapper.Request, linkedCts.Token)) {
                            var result = new HttpResponse {
                                ResourceId = httpRequest.ResourceId,
                                StatusCode = response.StatusCode,
                                Headers = response.Headers,
                                ContentHeaders = response.Content.Headers,
                                Content = await response.Content.ReadAsByteArrayAsync()
                            };
                            if (result.IsError()) {
                                _logger.Warning("{method} to {uri} returned {code} (took {elapsed}).",
                                    httpMethod, httpRequest.Uri, response.StatusCode, sw.Elapsed,
                                     result.GetContentAsString(Encoding.UTF8));
                            }
                            else {
                                _logger.Verbose("{method} to {uri} returned {code} (took {elapsed}).",
                                    httpMethod, httpRequest.Uri, response.StatusCode, sw.Elapsed);
                            }
                            return result;
                        }
                    }
                    catch (HttpRequestException e) {
                        var requestEx = generateHttpRequestException(e);
                        throw requestEx;
                    }
                    catch (OperationCanceledException e) {
                        if (ct.IsCancellationRequested) {
                            // Cancel was called. We will call ct.ThrowIfCancellationRequested() because the
                            // token that is passed to the exception is the linked token. This way,
                            // information about usage of linked tokens will not be leaked to the caller.
                            ct.ThrowIfCancellationRequested();
                        }

                        // Operation timed out.
                        var requestEx = generateHttpRequestException(e);
                        throw requestEx;
                    }
                }
            }
        }

        /// <summary>
        /// Request object
        /// </summary>
        public sealed class HttpRequest : IHttpRequest {

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="uri"></param>
            /// <param name="resourceId"></param>
            public HttpRequest(Uri uri, string resourceId) {
                Options = new HttpRequestOptions();
                Request = new HttpRequestMessage();
                if (!uri.Scheme.EqualsIgnoreCase("http") && !uri.Scheme.EqualsIgnoreCase("https")) {
                    // Need a way to work around request uri validation - add uds path to header.
                    Request.Headers.TryAddWithoutValidation(HttpHeader.UdsPath,
                        uri.ParseUdsPath(out uri));
                }
                Request.RequestUri = uri;
                ResourceId = resourceId;
                if (ResourceId != null) {
                    Request.Headers.TryAddWithoutValidation(HttpHeader.ResourceId, ResourceId);
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
        public sealed class HttpResponse : IHttpResponse {

            /// <inheritdoc/>
            public string ResourceId { get; internal set; }

            /// <inheritdoc/>
            public HttpStatusCode StatusCode { get; internal set; }

            /// <inheritdoc/>
            public HttpResponseHeaders Headers { get; internal set; }

            /// <inheritdoc/>
            public HttpContentHeaders ContentHeaders { get; internal set; }

            /// <inheritdoc/>
            public byte[] Content { get; internal set; }
        }

        private readonly IHttpClientFactory _factory;
        private readonly ILogger _logger;
    }
}
