// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Diagnostics;

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Http {

    public class HttpClient : IHttpClient
    {
        private readonly ILogger log;

        public HttpClient(ILogger logger)
        {
            this.log = logger;
        }

        public async Task<IHttpResponse> GetAsync(IHttpRequest request)
        {
            return await this.SendAsync(request, HttpMethod.Get);
        }

        public async Task<IHttpResponse> PostAsync(IHttpRequest request)
        {
            return await this.SendAsync(request, HttpMethod.Post);
        }

        public async Task<IHttpResponse> PutAsync(IHttpRequest request)
        {
            return await this.SendAsync(request, HttpMethod.Put);
        }

        public async Task<IHttpResponse> PatchAsync(IHttpRequest request)
        {
            return await this.SendAsync(request, new HttpMethod("PATCH"));
        }

        public async Task<IHttpResponse> DeleteAsync(IHttpRequest request)
        {
            return await this.SendAsync(request, HttpMethod.Delete);
        }

        public async Task<IHttpResponse> HeadAsync(IHttpRequest request)
        {
            return await this.SendAsync(request, HttpMethod.Head);
        }

        public async Task<IHttpResponse> OptionsAsync(IHttpRequest request)
        {
            return await this.SendAsync(request, HttpMethod.Options);
        }

        private async Task<IHttpResponse> SendAsync(IHttpRequest request, HttpMethod httpMethod)
        {
            var clientHandler = new HttpClientHandler();
            using (var client = new System.Net.Http.HttpClient(clientHandler))
            {
                var httpRequest = new HttpRequestMessage
                {
                    Method = httpMethod,
                    RequestUri = request.Uri
                };

                SetServerSSLSecurity(request, clientHandler);
                SetTimeout(request, client);
                SetContent(request, httpMethod, httpRequest);
                SetHeaders(request, httpRequest);

                this.log.Debug("Sending request", () => new { httpMethod, request.Uri, request.Options });

                try
                {
                    using (var response = await client.SendAsync(httpRequest))
                    {
                        if (request.Options.EnsureSuccess) response.EnsureSuccessStatusCode();

                        return new HttpResponse
                        {
                            StatusCode = response.StatusCode,
                            Headers = response.Headers,
                            Content = await response.Content.ReadAsStringAsync(),
                        };
                    }
                }
                catch (HttpRequestException e)
                {
                    var errorMessage = e.Message;
                    if (e.InnerException != null)
                    {
                        errorMessage += " - " + e.InnerException.Message;
                    }

                    this.log.Error("Request failed", () => new
                    {
                        ExceptionMessage = e.Message,
                        InnerExceptionType = e.InnerException != null ? e.InnerException.GetType().FullName : "",
                        InnerExceptionMessage = e.InnerException != null ? e.InnerException.Message : "",
                        errorMessage
                    });

                    return new HttpResponse
                    {
                        StatusCode = 0,
                        Content = errorMessage
                    };
                }
            }
        }

        private static void SetContent(IHttpRequest request, HttpMethod httpMethod, HttpRequestMessage httpRequest)
        {
            if (httpMethod != HttpMethod.Post && httpMethod != HttpMethod.Put && httpMethod.Method != "PATCH") return;

            httpRequest.Content = request.Content;
            if (request.ContentType != null && request.Content != null)
            {
                httpRequest.Content.Headers.ContentType = request.ContentType;
            }
        }

        private static void SetHeaders(IHttpRequest request, HttpRequestMessage httpRequest)
        {
            foreach (var header in request.Headers)
            {
                httpRequest.Headers.Add(header.Key, header.Value);
            }
        }

        private static void SetServerSSLSecurity(IHttpRequest request, HttpClientHandler clientHandler)
        {
            if (request.Options.AllowInsecureSSLServer)
            {
                clientHandler.ServerCertificateCustomValidationCallback = delegate { return true; };
            }
        }

        private static void SetTimeout(
            IHttpRequest request,
            System.Net.Http.HttpClient client)
        {
            client.Timeout = TimeSpan.FromMilliseconds(request.Options.Timeout);
        }
    }
}
