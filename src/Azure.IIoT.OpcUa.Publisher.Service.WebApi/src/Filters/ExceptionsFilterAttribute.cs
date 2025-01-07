// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Filters
{
    using Azure.IIoT.OpcUa.Exceptions;
    using Furly.Exceptions;
    using Furly.Tunnel.Exceptions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Diagnostics.ExceptionSummarization;
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
    public sealed class ExceptionsFilterAttribute : ExceptionFilterAttribute
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
            var summarizer = context.HttpContext?.RequestServices?
                .GetService<IExceptionSummarizer>();
            switch (context.Exception)
            {
                case ResourceNotFoundException:
                    context.Result = GetResponse(HttpStatusCode.NotFound,
                        context.Exception, summarizer);
                    break;
                case ResourceInvalidStateException:
                    context.Result = GetResponse(HttpStatusCode.Forbidden,
                        context.Exception, summarizer);
                    break;
                case ResourceConflictException:
                    context.Result = GetResponse(HttpStatusCode.Conflict,
                        context.Exception, summarizer);
                    break;
                case UnauthorizedAccessException:
                case SecurityException:
                    context.Result = GetResponse(HttpStatusCode.Unauthorized,
                        context.Exception, summarizer);
                    break;
                case MethodCallStatusException mcs:
                    context.Result = new ObjectResult(mcs.Details.ToProblemDetails());
                    break;
                case SerializerException:
                case MethodCallException:
                case BadRequestException:
                case ArgumentException:
                    context.Result = GetResponse(HttpStatusCode.BadRequest,
                        context.Exception, summarizer);
                    break;
                case NotImplementedException:
                case NotSupportedException:
                    context.Result = GetResponse(HttpStatusCode.NotImplemented,
                        context.Exception, summarizer);
                    break;
                case TimeoutException:
                    context.Result = GetResponse(HttpStatusCode.RequestTimeout,
                        context.Exception, summarizer);
                    break;
                case SocketException:
                case IOException:
                    context.Result = GetResponse(HttpStatusCode.BadGateway,
                        context.Exception, summarizer);
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

                case ServerBusyException:
                    context.Result = GetResponse(HttpStatusCode.TooManyRequests,
                        context.Exception, summarizer);
                    break;
                case ResourceOutOfDateException:
                    context.Result = GetResponse(HttpStatusCode.PreconditionFailed,
                        context.Exception, summarizer);
                    break;
                case ExternalDependencyException:
                    context.Result = GetResponse(HttpStatusCode.ServiceUnavailable,
                        context.Exception, summarizer);
                    break;
                default:
                    context.Result = GetResponse(HttpStatusCode.InternalServerError,
                        context.Exception, summarizer);
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
        /// <param name="summarizer"></param>
        /// <returns></returns>
        private static ObjectResult GetResponse(HttpStatusCode code, Exception exception,
            IExceptionSummarizer? summarizer)
        {
            if (summarizer != null)
            {
                var ex = exception.AsMethodCallStatusException((int)code, summarizer);
                return new ObjectResult(ex.Details.ToProblemDetails())
                {
                    StatusCode = (int)code
                };
            }

            return new ObjectResult(exception.Message)
            {
                StatusCode = (int)code
            };
        }
    }
}
