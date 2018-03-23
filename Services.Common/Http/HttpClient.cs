// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.Common.Http {
    using Microsoft.Azure.IoTSolutions.Common.Diagnostics;
    using System;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// Client implementation
    /// </summary>
    public class HttpClient : IHttpClient {

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="logger"></param>
        public HttpClient(ILogger logger) {
            _logger = logger;
        }

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
        /// <param name="request"></param>
        /// <param name="httpMethod"></param>
        /// <returns></returns>
        private async Task<IHttpResponse> SendAsync(IHttpRequest request,
            HttpMethod httpMethod) {
            var clientHandler = new HttpClientHandler();

            var sw = Stopwatch.StartNew();
            _logger.Debug($"Sending {httpMethod} request to {request.Uri}...",
                () => {
#if LOG_VERBOSE
                    return new { request.Headers, request.Content, request.Options };
#endif
                });
            using (var client = new System.Net.Http.HttpClient(clientHandler)) {
                var httpRequest = new HttpRequestMessage {
                    Method = httpMethod,
                    RequestUri = request.Uri
                };

                SetServerSSLSecurity(request, clientHandler);
                SetTimeout(request, client);
                SetContent(request, httpMethod, httpRequest);
                SetHeaders(request, httpRequest);
                try {
                    using (var response = await client.SendAsync(httpRequest)) {
                        var content = await response.Content.ReadAsStringAsync();
                        _logger.Debug(
                            $"... {httpMethod} to {request.Uri} returned " +
                            $"{response.StatusCode} (took {sw.Elapsed}).", () => {
#if LOG_VERBOSE
                                return new { response.Headers, content };
#endif
                            });
                        return new HttpResponse {
                            StatusCode = response.StatusCode,
                            Headers = response.Headers,
                            Content = content
                        };
                    }
                }
                catch (HttpRequestException e) {
                    var errorMessage = e.Message;
                    if (e.InnerException != null) {
                        errorMessage += " - " + e.InnerException.Message;
                    }
                    _logger.Error(
                        $"... {httpMethod} to {request.Uri} failed (took {sw.Elapsed})!",
                        () => new {
                            ExceptionMessage = e.Message,
                            InnerExceptionType = e.InnerException?.GetType().FullName ?? "",
                            InnerExceptionMessage = e.InnerException?.Message ?? "",
                            errorMessage
                        });
                    return new HttpResponse {
                        StatusCode = 0,
                        Content = errorMessage
                    };
                }
            }
        }

        /// <summary>
        /// Pass content
        /// </summary>
        /// <param name="request"></param>
        /// <param name="httpMethod"></param>
        /// <param name="httpRequest"></param>
        private static void SetContent(IHttpRequest request, HttpMethod httpMethod,
            HttpRequestMessage httpRequest) {
            if (httpMethod != HttpMethod.Post &&
                httpMethod != HttpMethod.Put &&
                httpMethod.Method != "PATCH") {
                return;
            }
            httpRequest.Content = request.Content;
            if (request.ContentType != null && request.Content != null) {
                httpRequest.Content.Headers.ContentType = request.ContentType;
            }
        }

        /// <summary>
        /// Set headers
        /// </summary>
        /// <param name="request"></param>
        /// <param name="httpRequest"></param>
        private static void SetHeaders(IHttpRequest request,
            HttpRequestMessage httpRequest) {
            foreach (var header in request.Headers) {
                httpRequest.Headers.Add(header.Key, header.Value);
            }
        }

        /// <summary>
        /// Enable ssl security
        /// </summary>
        /// <param name="request"></param>
        /// <param name="clientHandler"></param>
        private static void SetServerSSLSecurity(IHttpRequest request,
            HttpClientHandler clientHandler) {
            if (request.Options.AllowInsecureSSLServer) {
                clientHandler.ServerCertificateCustomValidationCallback =
                    (a, b, c, d) => true;
            }
        }

        /// <summary>
        /// Set request timeout
        /// </summary>
        /// <param name="request"></param>
        /// <param name="client"></param>
        private static void SetTimeout(
            IHttpRequest request,
            System.Net.Http.HttpClient client) {
#if DEBUG
            client.Timeout = TimeSpan.FromMilliseconds(request.Options.Timeout * 100);
#else
            client.Timeout = TimeSpan.FromMilliseconds(request.Options.Timeout);
#endif
        }

        private readonly ILogger _logger;
    }
}
