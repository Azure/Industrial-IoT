// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.Common.Http {
    using Microsoft.Azure.IoTSolutions.Common.Exceptions;
    using System;
    using System.Net;

    public static class HttpClientEx {

        public static void Validate(this IHttpResponse response) {
            switch (response.StatusCode) {
                case HttpStatusCode.OK:
                case HttpStatusCode.Created:
                case HttpStatusCode.Accepted:
                case HttpStatusCode.NonAuthoritativeInformation:
                case HttpStatusCode.NoContent:
                case HttpStatusCode.ResetContent:
                case HttpStatusCode.PartialContent:
                    break;
                case HttpStatusCode.MethodNotAllowed:
                    throw new InvalidOperationException(response.Content);
                case HttpStatusCode.NotAcceptable:
                case HttpStatusCode.BadRequest:
                    throw new BadRequestException(response.Content);
                case HttpStatusCode.Forbidden:
                case HttpStatusCode.Unauthorized:
                    throw new UnauthorizedAccessException(response.Content);
                case HttpStatusCode.NotFound:
                    throw new ResourceNotFoundException(response.Content);
                case HttpStatusCode.Conflict:
                    throw new ConflictingResourceException(response.Content);
                case HttpStatusCode.RequestTimeout:
                    throw new TimeoutException(response.Content);
                case HttpStatusCode.InternalServerError:
                case HttpStatusCode.BadGateway:
                case HttpStatusCode.ServiceUnavailable:
                case HttpStatusCode.GatewayTimeout:
                case HttpStatusCode.PreconditionFailed:
                case HttpStatusCode.TemporaryRedirect:
                case (HttpStatusCode)429:
                    // Retried
                    throw new HttpTransientException(response.StatusCode,
                        response.Content);
                default:
                    throw new HttpResponseException(response.StatusCode,
                        response.Content);
            }
        }
    }
}
