// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http {
    using Microsoft.Azure.IIoT.Http.Exceptions;
    using Microsoft.Azure.IIoT.Exceptions;
    using System;
    using System.Net;

    /// <summary>
    /// Http status code extensions
    /// </summary>
    public static class HttpStatusCodeEx {

        /// <summary>
        /// True if statusCode is an error
        /// </summary>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        public static bool IsError(this HttpStatusCode statusCode) {
            return (int)statusCode >= 400 || statusCode == 0;
        }

        /// <summary>
        /// Validate status code
        /// </summary>
        /// <param name="statusCode"></param>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public static void Validate(this HttpStatusCode statusCode,
            string message, Exception inner = null) {
            switch (statusCode) {
                case HttpStatusCode.OK:
                case HttpStatusCode.Created:
                case HttpStatusCode.Accepted:
                case HttpStatusCode.NonAuthoritativeInformation:
                case HttpStatusCode.NoContent:
                case HttpStatusCode.ResetContent:
                case HttpStatusCode.PartialContent:
                    break;
                case HttpStatusCode.MethodNotAllowed:
                    throw new InvalidOperationException(message, inner);
                case HttpStatusCode.NotAcceptable:
                case HttpStatusCode.BadRequest:
                    throw new BadRequestException(message, inner);
                case HttpStatusCode.Forbidden:
                    if (message.Contains("IotHubQuotaExceeded")) {
                        throw new IotHubQuotaExceededException(message, inner);
                    } 
                    else {
                        throw new ResourceInvalidStateException(message, inner);
                    }          
                case HttpStatusCode.Unauthorized:
                    throw new UnauthorizedAccessException(message, inner);
                case HttpStatusCode.NotFound:
                    throw new ResourceNotFoundException(message);
                case HttpStatusCode.Conflict:
                    throw new ConflictingResourceException(message, inner);
                case HttpStatusCode.RequestTimeout:
                    throw new TimeoutException(message, inner);
                case HttpStatusCode.PreconditionFailed:
                    throw new ResourceOutOfDateException(message, inner);
                case HttpStatusCode.InternalServerError:
                    if (message.Contains("IotHubQuotaExceeded")) {
                        throw new IotHubQuotaExceededException(message, inner);
                    }
                    else {
                        throw new ResourceInvalidStateException(message, inner);
                    }
                case HttpStatusCode.GatewayTimeout:
                case HttpStatusCode.ServiceUnavailable:
                case HttpStatusCode.TemporaryRedirect:
                    // Retried
                    throw new HttpTransientException(statusCode, message);
                case (HttpStatusCode)429:
                    // Retried
                    throw new HttpTransientException(statusCode, message);
                default:
                    throw new HttpResponseException(statusCode, message);
            }
        }
    }
}
