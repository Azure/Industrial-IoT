// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http.Exceptions {
    using System;
    using System.Net;

    /// <summary>
    /// Http request exception
    /// </summary>
    public class HttpResponseException : Exception {

        /// <summary>
        /// Create response exception
        /// </summary>
        /// <param name="statusCode"></param>
        public HttpResponseException(HttpStatusCode statusCode) {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Create response exception
        /// </summary>
        /// <param name="statusCode"></param>
        /// <param name="message"></param>
        public HttpResponseException(HttpStatusCode statusCode, string message) :
            base(message) {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Response status code
        /// </summary>
        public HttpStatusCode StatusCode { get; }
    }
}
