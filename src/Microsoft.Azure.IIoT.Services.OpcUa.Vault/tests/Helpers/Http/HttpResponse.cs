// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using System.Net;
using System.Net.Http.Headers;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.Test.Helpers.Http
{
    public interface IHttpResponse
    {
        HttpStatusCode StatusCode { get; }
        HttpResponseHeaders Headers { get; }
        string Content { get; }
        bool IsRetriableError { get; }
    }

    public class HttpResponse : IHttpResponse
    {
        private const int TooManyRequests = 429;

        public HttpResponse()
        {
        }

        public HttpResponse(
            HttpStatusCode statusCode,
            string content,
            HttpResponseHeaders headers)
        {
            this.StatusCode = statusCode;
            this.Headers = headers;
            this.Content = content;
        }

        public HttpStatusCode StatusCode { get; internal set; }
        public HttpResponseHeaders Headers { get; internal set; }
        public string Content { get; internal set; }

        public bool IsRetriableError => this.StatusCode == HttpStatusCode.NotFound ||
                                        this.StatusCode == HttpStatusCode.RequestTimeout ||
                                        (int) this.StatusCode == TooManyRequests;
    }
}
