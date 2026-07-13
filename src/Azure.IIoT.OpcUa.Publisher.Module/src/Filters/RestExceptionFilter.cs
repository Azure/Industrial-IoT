// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Filters
{
    using Azure.IIoT.OpcUa.Exceptions;
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using Azure.IIoT.OpcUa.Publisher.Module.Serialization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Diagnostics.ExceptionSummarization;
    using Microsoft.Extensions.Logging;
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Security;
    using System.Threading.Tasks;

    /// <summary>
    /// Minimal API endpoint filter that mirrors the behavior of the former MVC
    /// <see cref="ControllerExceptionFilterAttribute"/>. It detects unhandled
    /// exceptions produced by the endpoint handlers (which delegate into the
    /// same controller methods that back the direct method dispatch) and maps
    /// them to the exact same HTTP status codes and <see cref="ProblemDetails"/>
    /// response bodies as the MVC pipeline did. This preserves the REST wire
    /// behavior after the migration off <c>AddControllers()</c>.
    /// </summary>
    public sealed class RestExceptionFilter : IEndpointFilter
    {
        /// <inheritdoc/>
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context,
            EndpointFilterDelegate next)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(next);
            try
            {
                return await next(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return Map(ex, context.HttpContext);
            }
        }

        /// <summary>
        /// Map exception to a problem details result identical to the MVC filter.
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="httpContext"></param>
        private static IResult Map(Exception exception, HttpContext httpContext)
        {
            if (exception is AggregateException ae)
            {
                var root = ae.GetBaseException();
                exception = root is AggregateException && ae.InnerExceptions.Count > 0
                    ? ae.InnerExceptions[0]
                    : root;
            }

            var services = httpContext.RequestServices;
            var summarizer = services.GetService<IExceptionSummarizer>();
            var logger = services.GetService<ILoggerFactory>()?.CreateLogger(
                "Azure.IIoT.OpcUa.Publisher.Module.MethodCalls");

            IResult result;
            int status;
            switch (exception)
            {
                case ResourceNotFoundException:
                    (result, status) = Response(HttpStatusCode.NotFound, exception, summarizer);
                    break;
                case ResourceInvalidStateException:
                    (result, status) = Response(HttpStatusCode.Forbidden, exception, summarizer);
                    break;
                case ResourceConflictException:
                    (result, status) = Response(HttpStatusCode.Conflict, exception, summarizer);
                    break;
                case UnauthorizedAccessException:
                case SecurityException:
                    (result, status) = Response(HttpStatusCode.Unauthorized, exception, summarizer);
                    break;
                case MethodCallStatusException mcs:
                    var problem = mcs.Details.ToProblemDetails();
                    // Match the MVC ObjectResult with a null status code which
                    // surfaces as HTTP 200 with the problem details body.
                    result = Results.Json(problem, ModuleJsonContext.Default.ProblemDetails,
                        statusCode: null);
                    status = problem.Status ?? (int)HttpStatusCode.InternalServerError;
                    break;
                case SerializerException:
                case MethodCallException:
                case BadRequestException:
                case ArgumentException:
                    (result, status) = Response(HttpStatusCode.BadRequest, exception, summarizer);
                    break;
                case NotSupportedException:
                    (result, status) = Response(HttpStatusCode.MethodNotAllowed, exception, summarizer);
                    break;
                case NotImplementedException:
                    (result, status) = Response(HttpStatusCode.NotImplemented, exception, summarizer);
                    break;
                case TimeoutException:
                    (result, status) = Response(HttpStatusCode.RequestTimeout, exception, summarizer);
                    break;
                case SocketException:
                case IOException:
                    (result, status) = Response(HttpStatusCode.BadGateway, exception, summarizer);
                    break;
                case ServerBusyException:
                    (result, status) = Response(HttpStatusCode.TooManyRequests, exception, summarizer);
                    break;
                case ResourceOutOfDateException:
                    (result, status) = Response(HttpStatusCode.PreconditionFailed, exception, summarizer);
                    break;
                case ExternalDependencyException:
                    (result, status) = Response(HttpStatusCode.ServiceUnavailable, exception, summarizer);
                    break;
                default:
                    (result, status) = Response(HttpStatusCode.InternalServerError, exception, summarizer);
                    break;
            }
            LogFailure(logger, status, exception);
            return result;
        }

        /// <summary>
        /// Create result and status code
        /// </summary>
        /// <param name="code"></param>
        /// <param name="exception"></param>
        /// <param name="summarizer"></param>
        private static (IResult, int) Response(HttpStatusCode code, Exception exception,
            IExceptionSummarizer? summarizer)
        {
            if (summarizer != null)
            {
                var ex = exception.AsMethodCallStatusException((int)code, summarizer);
                return (Results.Json(ex.Details.ToProblemDetails(),
                    ModuleJsonContext.Default.ProblemDetails, statusCode: (int)code),
                    (int)code);
            }
            return (Results.Json(exception.Message, ModuleJsonContext.Default.String,
                statusCode: (int)code), (int)code);
        }

        /// <summary>
        /// Surface the failure so it is captured in logs and support bundles,
        /// using the exact same severity mapping as the MVC filter.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="status"></param>
        /// <param name="exception"></param>
        private static void LogFailure(ILogger? logger, int status, Exception exception)
        {
            if (logger == null)
            {
                return;
            }
            var level = status >= 500 ? LogLevel.Error
                : status is (int)HttpStatusCode.RequestTimeout
                    or (int)HttpStatusCode.TooManyRequests
                    or (int)HttpStatusCode.PreconditionFailed ? LogLevel.Warning
                : LogLevel.Debug;
            try
            {
                logger.RequestFailed(level, status, exception.GetType().Name, exception);
            }
            catch
            {
                // Diagnostics must never break exception-to-status mapping, e.g.
                // if a logging provider throws or has already been disposed.
            }
        }
    }
}
