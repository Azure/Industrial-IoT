// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.Shared.Http {
    using System;
    using System.Net;

    /// <summary>
    /// Http request exception
    /// </summary>
    public class HttpResponseException : Exception {
        public HttpResponseException(HttpStatusCode statusCode) {
            StatusCode = statusCode;
        }

        public HttpResponseException(HttpStatusCode statusCode, string message) :
            base(message) {
            StatusCode = statusCode;
        }

        public HttpStatusCode StatusCode { get; }
    }
}
