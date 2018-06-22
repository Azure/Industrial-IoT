// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http.Proxy {
    using Microsoft.Azure.IIoT.Http.Proxy.Exceptions;
    using Microsoft.Azure.IIoT.Services.Http.Proxy;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Extensions.Primitives;
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using HttpRequest = Microsoft.AspNetCore.Http.HttpRequest;
    using HttpResponse = Microsoft.AspNetCore.Http.HttpResponse;

    public class ReverseProxy : IProxy {

        /// <summary>
        /// Create proxy
        /// </summary>
        /// <param name="client"></param>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        public ReverseProxy(IHttpClient client, IReverseProxyConfig configuration,
            ILogger logger) {
            _client = client;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Process request and fill response
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        public async Task ForwardAsync(HttpRequest request, HttpResponse response) {
            response.DisableSessionAffinity();
            try {
                var httpRequest = CreateRequest(request);
                var httpResponse = await ForwardAsync(request.Method,
                    httpRequest);
                await WriteResponseAsync(httpResponse, response);
            }
            catch (ResourceNotFoundException) {
                response.StatusCode = (int)HttpStatusCode.NotFound;
            }
            catch (HttpPayloadTooLargeException) {
                response.StatusCode = (int)HttpStatusCode.RequestEntityTooLarge;
            }
            catch (NotSupportedException) {
                response.StatusCode = (int)HttpStatusCode.NotImplemented;
            }
            catch (Exception) {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
        }

        /// <summary>
        /// Forward the request
        /// </summary>
        /// <param name="method"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        private Task<IHttpResponse> ForwardAsync(string method, IHttpRequest request) {
            switch (method.ToUpperInvariant()) {
                case "GET":
                    return _client.GetAsync(request);
                case "DELETE":
                    return _client.DeleteAsync(request);
                case "OPTIONS":
                    return _client.OptionsAsync(request);
                case "HEAD":
                    return _client.HeadAsync(request);
                case "POST":
                    return _client.PostAsync(request);
                case "PUT":
                    return _client.PutAsync(request);
                case "PATCH":
                    return _client.PatchAsync(request);
                default:
                    throw new NotSupportedException($"{method} is not supported");
            }
        }

        /// <summary>
        /// Lookup endpoint to use for request. Either the resource id is
        /// passed as x-resource-id header, or as first segment in the path.
        /// If the host and port are part of the lookup we return it, otherwise
        /// we treat the resource id as host name.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="resourceId"></param>
        /// <returns></returns>
        private string LookupEndpoint(HttpRequest request, out string resourceId) {
            resourceId = null;
            if (request.Headers.TryGetValue(HttpHeader.ResourceId,
                out var resourceHeader)) {
                resourceId = resourceHeader.FirstOrDefault();
            }
            if (string.IsNullOrEmpty(resourceId)) {
                // Get from path
                var path = ((string)request.Path).TrimStart('/');
                var index = path.IndexOf('/');
                if (index <= 0) {
                    throw new ResourceNotFoundException(
                        "Path does not contain resource id");
                }
                request.Path = path.Substring(index);
                resourceId = path.Substring(0, index);
                if (string.IsNullOrEmpty(resourceId)) {
                    throw new ResourceNotFoundException(
                        "Remote endpoint not found");
                }
            }
            var lookup = _configuration.ResourceIdToHostLookup;
            string remoteEndpoint;
            if (lookup != null && lookup.Count > 0) {
                if (lookup.TryGetValue(resourceId, out remoteEndpoint) &&
                    !string.IsNullOrEmpty(remoteEndpoint)) {
                    return remoteEndpoint;
                }
            }
            remoteEndpoint = resourceId;
            resourceId = null;
            return remoteEndpoint;
        }

        /// <summary>
        /// Build request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private IHttpRequest CreateRequest(HttpRequest request) {

            var url = new UriBuilder {
                Host = LookupEndpoint(request, out var resourceId),
                Path = request.Path.Value,
                Query = request.QueryString.Value,
                Scheme = "https"
            };
            var httpRequest = _client.NewRequest(url.Uri, resourceId);

            // Copy headers
            foreach (var header in request.Headers) {
                switch (header.Key.ToLowerInvariant()) {
                    case HttpHeader.ResourceId:
                    case HttpHeader.SourceId:
                    case "connection":
                    case "content-length":
                    case "keep-alive":
                    case "host":
                    case "upgrade":
                    case "upgrade-insecure-requests":
                        break;
                    default:
                        foreach (var value in header.Value) {
                            httpRequest.AddHeader(header.Key, value);
                        }
                        break;
                }
            }

            // Copy payload
            switch(request.Method.ToUpperInvariant()) {
                case "POST":
                case "PATCH":
                case "PUT":
                    using (var stream = new MemoryStream()) {
                        request.Body.CopyTo(stream);
                        httpRequest.SetContent(stream.ToArray(), 
                            request.ContentType);
                    }
                    break;
            }
            return httpRequest;
        }

        /// <summary>
        /// Create response
        /// </summary>
        /// <param name="httpResponse"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        private async Task WriteResponseAsync(IHttpResponse httpResponse,
            HttpResponse response) {

            response.StatusCode = (int)httpResponse.StatusCode;
            // The Headers property can be null in case of errors
            if (httpResponse.Headers != null) {
                foreach (var header in httpResponse.Headers) {
                    switch(header.Key.ToLowerInvariant()) {
                        case "connection":
                        case "server":
                        case "transfer-encoding":
                        case "upgrade":
                        case "x-powered-by":
                        case "strict-transport-security":
                            break;
                        default:
                            // Forward the HTTP headers
                            response.AddHeaders(header.Key, header.Value);
                            break;
                    }
                }
            }
            if (httpResponse.CanHaveBody()) {
                var content = httpResponse.Content;
                if (content.Length > 0) {
                    await response.Body.WriteAsync(content, 0, content.Length);
                }
            }
        }
        private readonly IHttpClient _client;
        private readonly IReverseProxyConfig _configuration;
        private readonly ILogger _logger;
    }
}
