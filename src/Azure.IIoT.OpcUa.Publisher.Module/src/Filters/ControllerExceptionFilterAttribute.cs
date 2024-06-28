// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Filters
{
    using Azure.IIoT.OpcUa.Exceptions;
    using Furly.Exceptions;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Security;
    using System.Threading.Tasks;

    /// <summary>
    /// Detect all the unhandled exceptions returned by the API controllers
    /// and decorate the response accordingly, managing the HTTP status code
    /// and preparing a JSON response with useful error details.
    /// When including the stack trace, split the text in multiple lines
    /// for an easier parsing.
    /// @see https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/filters
    /// </summary>
    public sealed class ControllerExceptionFilterAttribute : ExceptionFilterAttribute
    {
        /// <inheritdoc />
        public override void OnException(ExceptionContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context.Exception == null)
            {
                base.OnException(context);
                return;
            }
            if (context.Exception is AggregateException ae)
            {
                var root = ae.GetBaseException();
                if (root is AggregateException && ae.InnerExceptions.Count > 0)
                {
                    context.Exception = ae.InnerExceptions[0];
                }
                else
                {
                    context.Exception = root;
                }
            }
            switch (context.Exception)
            {
                case ResourceNotFoundException re:
                    context.Result = GetResponse(HttpStatusCode.NotFound,
                        context.Exception);
                    break;
                case ResourceInvalidStateException ri:
                    context.Result = GetResponse(HttpStatusCode.Forbidden,
                        context.Exception);
                    break;
                case ResourceConflictException ce:
                    context.Result = GetResponse(HttpStatusCode.Conflict,
                        context.Exception);
                    break;
                case UnauthorizedAccessException ue:
                case SecurityException se:
                    context.Result = GetResponse(HttpStatusCode.Unauthorized,
                        context.Exception);
                    break;
                case MethodCallStatusException mcs:
                    context.Result = GetResponse((HttpStatusCode)mcs.Result,
                        context.Exception);
                    break;
                case SerializerException sre:
                case MethodCallException mce:
                case BadRequestException br:
                case ArgumentException are:
                    context.Result = GetResponse(HttpStatusCode.BadRequest,
                        context.Exception);
                    break;
                case NotSupportedException ns:
                    context.Result = GetResponse(HttpStatusCode.MethodNotAllowed,
                        context.Exception);
                    break;
                case NotImplementedException ne:
                    context.Result = GetResponse(HttpStatusCode.NotImplemented,
                        context.Exception);
                    break;
                case TimeoutException te:
                    context.Result = GetResponse(HttpStatusCode.RequestTimeout,
                        context.Exception);
                    break;
                case SocketException sex:
                case IOException ce:
                    context.Result = GetResponse(HttpStatusCode.BadGateway,
                        context.Exception);
                    break;

                //
                // The following will most certainly be retried by our
                // service client implementations and thus dependent
                // services:
                //
                //      InternalServerError
                //      BadGateway
                //      ServiceUnavailable
                //      GatewayTimeout
                //      PreconditionFailed
                //      TemporaryRedirect
                //      TooManyRequests
                //
                // As such, if you want to terminate make sure exception
                // is caught ahead of here and returns a status other than
                // one of the above.
                //

                case ServerBusyException se:
                    context.Result = GetResponse(HttpStatusCode.TooManyRequests,
                        context.Exception);
                    break;
                case ResourceOutOfDateException re:
                    context.Result = GetResponse(HttpStatusCode.PreconditionFailed,
                        context.Exception);
                    break;
                case ExternalDependencyException ex:
                    context.Result = GetResponse(HttpStatusCode.ServiceUnavailable,
                        context.Exception);
                    break;
                default:
                    context.Result = GetResponse(HttpStatusCode.InternalServerError,
                        context.Exception);
                    break;
            }
        }

        /// <inheritdoc />
        public override Task OnExceptionAsync(ExceptionContext context)
        {
            try
            {
                OnException(context);
                return Task.CompletedTask;
            }
            catch (Exception)
            {
                return base.OnExceptionAsync(context);
            }
        }

        /// <summary>
        /// Create result
        /// </summary>
        /// <param name="code"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        private static ObjectResult GetResponse(HttpStatusCode code, Exception exception)
        {
            return new ObjectResult(exception.Message)
            {
                StatusCode = (int)code
            };
        }
    }
}
