// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http.Default
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Http client wrapping http client factory created http clients and
    /// abstracting away all the http client factory and handler noise
    /// for easy injection.
    /// </summary>
    public sealed class HttpClient : IHttpClient
    {
        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="logger"></param>
        public HttpClient(ILogger logger) :
            this(null, logger)
        {
        }

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="logger"></param>
        public HttpClient(IHttpClientFactory factory, ILogger logger)
        {
            _factory = factory ?? new HttpClientFactory(null, logger);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        /// <inheritdoc/>
        public IHttpRequest NewRequest(Uri uri, string resourceId)
        {
            return new HttpRequest(uri, resourceId);
        }

        /// <inheritdoc/>
        public Task<IHttpResponse> GetAsync(IHttpRequest request, CancellationToken ct)
        {
            return SendAsync(request, HttpMethod.Get, ct);
        }

        /// <inheritdoc/>
        public Task<IHttpResponse> PostAsync(IHttpRequest request, CancellationToken ct)
        {
            return SendAsync(request, HttpMethod.Post, ct);
        }

        /// <inheritdoc/>
        public Task<IHttpResponse> PutAsync(IHttpRequest request, CancellationToken ct)
        {
            return SendAsync(request, HttpMethod.Put, ct);
        }

        /// <inheritdoc/>
        public Task<IHttpResponse> PatchAsync(IHttpRequest request, CancellationToken ct)
        {
            return SendAsync(request, new HttpMethod("PATCH"), ct);
        }

        /// <inheritdoc/>
        public Task<IHttpResponse> DeleteAsync(IHttpRequest request, CancellationToken ct)
        {
            return SendAsync(request, HttpMethod.Delete, ct);
        }

        /// <summary>
        /// Send request
        /// </summary>
        /// <param name="httpRequest"></param>
        /// <param name="httpMethod"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="HttpRequestException"></exception>
        private async Task<IHttpResponse> SendAsync(IHttpRequest httpRequest,
            HttpMethod httpMethod, CancellationToken ct)
        {
            if (!(httpRequest is HttpRequest wrapper))
            {
                throw new InvalidOperationException("Bad request");
            }

            using (var client = _factory.CreateClient(httpRequest.ResourceId ??
                HttpHandlerFactory.DefaultResourceId))
            {
                if (httpRequest.Options.Timeout.HasValue)
                {
                    client.Timeout = httpRequest.Options.Timeout.Value;
                }

                var sw = Stopwatch.StartNew();
                _logger.LogTrace("Sending {Method} request to {Uri}...", httpMethod,
                    httpRequest.Uri);

                // We will use this local function for Exception formatting
                HttpRequestException generateHttpRequestException(Exception e)
                {
                    var errorMessage = $"{e.GetType()}: {e.Message}";
                    if (e.InnerException != null)
                    {
                        errorMessage += " - " + e.InnerException.Message;
                    }
                    if (!httpRequest.Options.SuppressHttpClientLogging)
                    {
                        _logger.LogWarning("{Method} to {Uri} failed (after {Elapsed}) : {Message}!",
                            httpMethod, httpRequest.Uri, sw.Elapsed, errorMessage);
                    }
                    _logger.LogDebug(e, "{Method} to {Uri} failed (after {Elapsed}) : {Message}!",
                        httpMethod, httpRequest.Uri, sw.Elapsed, errorMessage);
                    return new HttpRequestException(errorMessage, e);
                }

                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                {
                    try
                    {
                        wrapper.Request.Method = httpMethod;
                        using (var response = await client.SendAsync(wrapper.Request, linkedCts.Token).ConfigureAwait(false))
                        {
                            var result = new HttpResponse
                            {
                                ResourceId = httpRequest.ResourceId,
                                StatusCode = response.StatusCode,
                                Headers = response.Headers,
                                ContentHeaders = response.Content.Headers,
                                Content = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false)
                            };
                            if (result.IsError())
                            {
                                if (!httpRequest.Options.SuppressHttpClientLogging)
                                {
                                    _logger.LogWarning("{Method} to {Uri} returned {Code} (took {Elapsed}) {Error}.",
                                        httpMethod, httpRequest.Uri, response.StatusCode, sw.Elapsed,
                                         result.GetContentAsString(Encoding.UTF8));
                                }
                                else
                                {
                                    _logger.LogDebug("{Method} to {Uri} returned {Code} (took {Elapsed}).",
                                        httpMethod, httpRequest.Uri, response.StatusCode, sw.Elapsed);
                                }
                            }
                            else
                            {
                                _logger.LogTrace("{Method} to {Uri} returned {Code} (took {Elapsed}).",
                                    httpMethod, httpRequest.Uri, response.StatusCode, sw.Elapsed);
                            }
                            return result;
                        }
                    }
                    catch (HttpRequestException e)
                    {
                        var requestEx = generateHttpRequestException(e);
                        throw requestEx;
                    }
                    catch (OperationCanceledException e)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            // Cancel was called. We will call ct.ThrowIfCancellationRequested() because the
                            // token that is passed to the exception is the linked token. This way,
                            // information about usage of linked tokens will not be leaked to the caller.
                            ct.ThrowIfCancellationRequested();
                        }

                        // Operation timed out.
                        var requestEx = generateHttpRequestException(e);
                        throw requestEx;
                    }
                    catch (Exception ex)
                    {
                        if (!httpRequest.Options.SuppressHttpClientLogging)
                        {
                            _logger.LogWarning("{Method} to {Uri} failed (after {Elapsed}) : {Message}!",
                                httpMethod, httpRequest.Uri, sw.Elapsed, ex.Message);
                        }
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Request object
        /// </summary>
        public sealed class HttpRequest : IHttpRequest
        {
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="uri"></param>
            /// <param name="resourceId"></param>
            public HttpRequest(Uri uri, string resourceId)
            {
                Options = new Http.HttpRequestOptions();
                Request = new HttpRequestMessage();
                if (!uri.Scheme.EqualsIgnoreCase("http") && !uri.Scheme.EqualsIgnoreCase("https"))
                {
                    // Need a way to work around request uri validation - add uds path to header.
                    Request.Headers.TryAddWithoutValidation(HttpHeader2.UdsPath,
                        ParseUdsPath(uri, out uri));
                }
                Request.RequestUri = uri;
                ResourceId = resourceId;
                if (ResourceId != null)
                {
                    Request.Headers.TryAddWithoutValidation(HttpHeader2.ResourceId, ResourceId);
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
            public Http.HttpRequestOptions Options { get; }

            /// <inheritdoc/>
            public HttpContent Content
            {
                get => Request.Content;
                set => Request.Content = value;
            }

            /// <inheritdoc/>
            public string ResourceId { get; }

            /// <summary>
            /// Parse uds uri
            /// </summary>
            /// <param name="fileUri"></param>
            /// <param name="httpRequestUri"></param>
            private static string ParseUdsPath(Uri fileUri, out Uri httpRequestUri)
            {
                var localPath = fileUri.LocalPath;
                // Find socket
                var builder = new UriBuilder(fileUri)
                {
                    Scheme = "https",
                    Host = Dns.GetHostName()
                };
                string fileDevice;
                string pathAndQuery;
                var index = localPath.IndexOf("sock", StringComparison.InvariantCultureIgnoreCase);
                if (index != -1)
                {
                    fileDevice = localPath.Substring(0, index + 4);
                    pathAndQuery = localPath.Substring(index + 4);
                }
                else
                {
                    // Find fake port delimiter
                    index = localPath.IndexOf(':', StringComparison.Ordinal);
                    if (index != -1)
                    {
                        fileDevice = localPath.Substring(0, index);
                        pathAndQuery = localPath.Substring(index + 1);
                    }
                    else
                    {
                        builder.Path = "/";
                        httpRequestUri = builder.Uri;
                        return localPath.TrimEnd('/');
                    }
                }

                // Find first path character and strip off everything before...
                index = pathAndQuery.IndexOf('/', StringComparison.Ordinal);
                if (index > 0)
                {
                    pathAndQuery = pathAndQuery.Substring(index, pathAndQuery.Length - index);
                }
                builder.Path = pathAndQuery;
                httpRequestUri = builder.Uri;
                return fileDevice;
            }
        }

        /// <summary>
        /// Response object
        /// </summary>
        public sealed class HttpResponse : IHttpResponse
        {
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
