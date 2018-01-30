// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Net;

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Http 
{
    /// <summary>
    /// Http request exception
    /// </summary>
    public class HttpResponseException : Exception
    {
        public HttpResponseException(HttpStatusCode statusCode)
        {
            StatusCode = statusCode;
        }

        public HttpResponseException(HttpStatusCode statusCode, string message) :
            base(message)
        {
            StatusCode = statusCode;
        }

        public HttpStatusCode StatusCode { get; }
    }
}
