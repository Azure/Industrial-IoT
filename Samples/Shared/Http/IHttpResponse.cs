// Copyright (c) Microsoft. All rights reserved.

using System.Net;
using System.Net.Http.Headers;

namespace Microsoft.Azure.IoTSolutions.Shared.Http{
    public interface IHttpResponse
    {
        HttpStatusCode StatusCode { get; }
        HttpResponseHeaders Headers { get; }
        string Content { get; }
        bool IsRetriableError { get; }
    }
}
