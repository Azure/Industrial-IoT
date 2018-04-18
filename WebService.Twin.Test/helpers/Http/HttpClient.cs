// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace WebService.Test.helpers.Http
{
    public interface IHttpClient
    {
        Task<IHttpResponse> GetAsync(IHttpRequest request);

        Task<IHttpResponse> PostAsync(IHttpRequest request);

        Task<IHttpResponse> PutAsync(IHttpRequest request);

        Task<IHttpResponse> PatchAsync(IHttpRequest request);

        Task<IHttpResponse> DeleteAsync(IHttpRequest request);

        Task<IHttpResponse> HeadAsync(IHttpRequest request);

        Task<IHttpResponse> OptionsAsync(IHttpRequest request);
    }

    public class HttpClient : IHttpClient
    {
        private readonly ITestOutputHelper log;
        private readonly bool logCanContainCredentials;

        public HttpClient()
        {
        }

        public HttpClient(ITestOutputHelper logger)
        {
            this.log = logger;

            try
            {
                // See team wiki /wiki/Environment-variables
                var envSetting = Environment
                    .GetEnvironmentVariable("PCS_ENABLE_UNSAFE_LOGS")
                    .ToLowerInvariant();
                this.logCanContainCredentials = envSetting == "true";
            }
            catch (Exception)
            {
                this.logCanContainCredentials = false;
            }
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
            this.LogRequest(request, httpMethod);

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

                using (var response = await client.SendAsync(httpRequest))
                {
                    if (request.Options.EnsureSuccess) response.EnsureSuccessStatusCode();

                    IHttpResponse result = new HttpResponse
                    {
                        StatusCode = response.StatusCode,
                        Headers = response.Headers,
                        Content = await response.Content.ReadAsStringAsync(),
                    };

                    this.LogResponse(result);

                    return result;
                }
            }
        }

        private static void SetContent(IHttpRequest request, HttpMethod httpMethod, HttpRequestMessage httpRequest)
        {
            if (httpMethod != HttpMethod.Post && httpMethod != HttpMethod.Put) return;

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

        private void LogRequest(IHttpRequest request, HttpMethod httpMethod)
        {
            if (this.log == null) return;

            this.log.WriteLine("### REQUEST ##############################");
            this.log.WriteLine("# Method: " + httpMethod);
            this.log.WriteLine("# URI: " + request.Uri);
            this.log.WriteLine("# Timeout: " + request.Options.Timeout);

            var headers = this.HeadersToString(request.Headers);
            if (httpMethod == HttpMethod.Post || httpMethod == HttpMethod.Put)
            {
                headers = headers.Trim() + this.HeadersToString(request.Content.Headers);
            }
            this.log.WriteLine("# Headers:\n" + headers);

            if (httpMethod == HttpMethod.Post || httpMethod == HttpMethod.Put)
            {
                this.LogContent(request.Content.ReadAsStringAsync().Result);
            }
        }

        private void LogResponse(IHttpResponse response)
        {
            if (this.log == null) return;

            this.log.WriteLine("### RESPONSE ##############################");
            this.log.WriteLine("# Status code: " + (int) response.StatusCode + " " + response.StatusCode);
            this.log.WriteLine("# Headers:\n" + this.HeadersToString(response.Headers));
            this.LogContent(response.Content);
        }

        private void LogContent(string content)
        {
            if (this.logCanContainCredentials)
            {
                this.log.WriteLine("# Content: **LOG ENABLED** (see dev. setup to hide sensitive content)");
                try
                {
                    var o = JsonConvert.DeserializeObject(content);
                    var s = JsonConvert.SerializeObject(o, Formatting.Indented);
                    this.log.WriteLine(s);
                }
                catch (Exception)
                {
                    this.log.WriteLine(content);
                }
            }
            else
            {
                this.log.WriteLine("# Content: **HIDDEN** (see dev. setup to unhide sensitive content)");
            }
        }

        private string HeadersToString(HttpHeaders h)
        {
            // Headers not to log in unsafe/shared environments (e.g. CI)
            var restricted = new List<string> { "authorization", "proxy-authorization", "cookie", "set-cookie" };

            var result = "";
            foreach (var pair in h)
            {
                if (this.logCanContainCredentials || !restricted.Contains(pair.Key.ToLowerInvariant()))
                {
                    result = pair.Value.Aggregate(result, (current, s) => current + "  " + pair.Key + ": " + s + "\n");
                }
                else
                {
                    result = result + "  " + pair.Key + ": **HIDDEN** (see dev. setup to unhide sensitive content)\n";
                }
            }
            return result.Trim('\n');
        }
    }
}
