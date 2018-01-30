// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Exceptions;
using System.Net;

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Http {
    /// <summary>
    /// Retriable exception
    /// </summary>
    public class HttpTransientException : HttpResponseException, ITransientException {

        public HttpTransientException(HttpStatusCode statusCode) : base(statusCode) {
        }

        public HttpTransientException(HttpStatusCode statusCode, string message) : base(statusCode, message) {
        }
    }
}
