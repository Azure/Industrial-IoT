// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http {
    using Microsoft.Azure.IIoT.Http.Exceptions;
    using Microsoft.Azure.IIoT.Exceptions;
    using System;
    using System.Net;
    using System.Text;

    public static class HttpResponseEx {

        /// <summary>
        /// Response content
        /// </summary>
        public static string GetContentAsString(this IHttpResponse response,
            Encoding encoding) => encoding.GetString(response.Content);

        /// <summary>
        /// Response content
        /// </summary>
        public static string GetContentAsString(this IHttpResponse response) =>
            GetContentAsString(response, Encoding.UTF8);

        /// <summary>
        /// Validate response
        /// </summary>
        /// <param name="response"></param>
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
                    throw new InvalidOperationException(response.GetContentAsString());
                case HttpStatusCode.NotAcceptable:
                case HttpStatusCode.BadRequest:
                    throw new BadRequestException(response.GetContentAsString());
                case HttpStatusCode.Forbidden:
                case HttpStatusCode.Unauthorized:
                    throw new UnauthorizedAccessException(response.GetContentAsString());
                case HttpStatusCode.NotFound:
                    throw new ResourceNotFoundException(response.GetContentAsString());
                case HttpStatusCode.Conflict:
                    throw new ConflictingResourceException(response.GetContentAsString());
                case HttpStatusCode.RequestTimeout:
                    throw new TimeoutException(response.GetContentAsString());
                case HttpStatusCode.PreconditionFailed:
                    throw new ResourceOutOfDateException(response.GetContentAsString());
                case HttpStatusCode.InternalServerError:
                case HttpStatusCode.GatewayTimeout:
                case HttpStatusCode.TemporaryRedirect:
                case (HttpStatusCode)429:
                    // Retried
                    throw new HttpTransientException(response.StatusCode,
                        response.GetContentAsString());
                default:
                    throw new HttpResponseException(response.StatusCode,
                        response.GetContentAsString());
            }
        }

        public static bool IsBadRequest(this IHttpResponse response) =>
            response.StatusCode == HttpStatusCode.BadRequest;
        public static bool IsUnauthorized(this IHttpResponse response) =>
            response.StatusCode == HttpStatusCode.Unauthorized;
        public static bool IsForbidden(this IHttpResponse response) =>
            response.StatusCode == HttpStatusCode.Forbidden;
        public static bool IsNotFound(this IHttpResponse response) =>
            response.StatusCode == HttpStatusCode.NotFound;
        public static bool IsTimeout(this IHttpResponse response) =>
            response.StatusCode == HttpStatusCode.RequestTimeout;
        public static bool IsConflict(this IHttpResponse response) =>
            response.StatusCode == HttpStatusCode.Conflict;
        public static bool IsServerError(this IHttpResponse response) =>
            response.StatusCode >= HttpStatusCode.InternalServerError;
        public static bool IsServiceUnavailable(this IHttpResponse response) =>
            response.StatusCode == HttpStatusCode.ServiceUnavailable;

        /// <summary>
        /// Response is not completed
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static bool IsIncomplete(this IHttpResponse response) {
            var status = (int)response.StatusCode;
            return
                (status >= 100 && status <= 199) ||
                (status >= 300 && status <= 399);
        }

        /// <summary>
        /// Check whether the response could have a body
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static bool CanHaveBody(this IHttpResponse response) {
            switch (response.StatusCode) {
                case HttpStatusCode.NotModified:
                case HttpStatusCode.NoContent:
                case HttpStatusCode.ResetContent:
                    // HTTP status codes without a body - see RFC 2616
                    return false;
                default:
                    return true;
            }
        }

        public static bool IsSuccess(this IHttpResponse response) =>
            (int)response.StatusCode >= 200 && (int)response.StatusCode <= 299;
        public static bool IsError(this IHttpResponse response) =>
            (int)response.StatusCode >= 400 || response.StatusCode == 0;

        /// <summary>
        /// Error and cannot be retried.
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static bool IsNonRetriableError(this IHttpResponse response) =>
            response.IsError() && !response.IsRetriableError();

        /// <summary>
        /// Technically can be retried
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static bool IsRetriableError(this IHttpResponse response) {
            switch (response.StatusCode) {
                case HttpStatusCode.NotFound:
                case HttpStatusCode.RequestTimeout:
                case (HttpStatusCode)429:
                    return true;
                default:
                    return false;
            }
        }
    }
}
