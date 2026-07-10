// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients
{
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Text;

    /// <summary>
    /// HTTP helpers for event clients.
    /// </summary>
    internal static class HttpClientExtensions
    {
        /// <summary>
        /// Add header without validation.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static HttpRequestMessage AddHeader(this HttpRequestMessage request,
            string name, string? value)
        {
            if (!request.Headers.TryAddWithoutValidation(name, value) &&
                !name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentOutOfRangeException(name, "Invalid header name");
            }
            return request;
        }

        /// <summary>
        /// Validate response status.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="throwOnError"></param>
        /// <returns></returns>
        public static bool ValidateResponse(this HttpResponseMessage response,
            bool throwOnError = true)
        {
            if ((int)response.StatusCode < 400 && response.StatusCode != 0)
            {
                return true;
            }
            if (!throwOnError)
            {
                return false;
            }
            switch (response.StatusCode)
            {
                case HttpStatusCode.MethodNotAllowed:
                    throw new InvalidOperationException(Message(response));
                case HttpStatusCode.NotAcceptable:
                case HttpStatusCode.BadRequest:
                    throw new BadRequestException(Message(response));
                case HttpStatusCode.Forbidden:
                    throw new ResourceInvalidStateException(Message(response));
                case HttpStatusCode.Unauthorized:
                    throw new UnauthorizedAccessException(Message(response));
                case HttpStatusCode.NotFound:
                    throw new ResourceNotFoundException(Message(response));
                case HttpStatusCode.Conflict:
                    throw new ResourceConflictException(Message(response));
                case HttpStatusCode.RequestTimeout:
                    throw new TimeoutException(Message(response));
                case HttpStatusCode.PreconditionFailed:
                    throw new ResourceOutOfDateException(Message(response));
                case HttpStatusCode.InternalServerError:
                    throw new ResourceInvalidStateException(Message(response));
                case HttpStatusCode.GatewayTimeout:
                case HttpStatusCode.ServiceUnavailable:
                case HttpStatusCode.TemporaryRedirect:
                case HttpStatusCode.TooManyRequests:
                    throw new HttpTransientException(response.StatusCode, Message(response));
                default:
                    throw new HttpRequestException(Message(response), null, response.StatusCode);
            }

            static string Message(HttpResponseMessage response)
            {
                try
                {
                    var buffer = response.Content.ReadAsByteArrayAsync()
                        .GetAwaiter().GetResult();
                    return Encoding.UTF8.GetString(buffer);
                }
                catch
                {
                    return response.StatusCode.ToString();
                }
            }
        }

        /// <summary>
        /// Retriable HTTP exception.
        /// </summary>
        private sealed class HttpTransientException : HttpRequestException, ITransientException
        {
            /// <summary>
            /// Create exception.
            /// </summary>
            /// <param name="statusCode"></param>
            /// <param name="message"></param>
            public HttpTransientException(HttpStatusCode statusCode, string message) :
                base(message, null, statusCode)
            {
            }
        }
    }
}
