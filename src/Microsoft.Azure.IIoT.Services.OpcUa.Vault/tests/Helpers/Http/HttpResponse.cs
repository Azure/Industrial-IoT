// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.Tests.Helpers.Http {
    using System.Net;
    using System.Net.Http.Headers;

    public interface IHttpResponse {
        HttpStatusCode StatusCode { get; }
        HttpResponseHeaders Headers { get; }
        string Content { get; }
        bool IsRetriableError { get; }
    }

    public class HttpResponse : IHttpResponse {
        private const int TooManyRequests = 429;

        public HttpResponse() {
        }

        public HttpResponse(
            HttpStatusCode statusCode,
            string content,
            HttpResponseHeaders headers) {
            StatusCode = statusCode;
            Headers = headers;
            Content = content;
        }

        public HttpStatusCode StatusCode { get; internal set; }
        public HttpResponseHeaders Headers { get; internal set; }
        public string Content { get; internal set; }

        public bool IsRetriableError => StatusCode == HttpStatusCode.NotFound ||
                                        StatusCode == HttpStatusCode.RequestTimeout ||
                                        (int)StatusCode == TooManyRequests;
    }
}
