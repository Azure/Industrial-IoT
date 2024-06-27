// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Filters
{
    using Azure.IIoT.OpcUa.Exceptions;
    using Furly.Exceptions;
    using Furly.Tunnel.Router;
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Security;
    using System.Threading.Tasks;

    /// <summary>
    /// Convert all the exceptions returned by the module controllers to a
    /// status code.
    /// </summary>
    public sealed class RouterExceptionFilterAttribute : ExceptionFilterAttribute
    {
        /// <inheritdoc />
        public override Exception Filter(Exception exception, out int status)
        {
            switch (exception)
            {
                case AggregateException ae:
                    var root = ae.GetBaseException();
                    if (root is not AggregateException ae2)
                    {
                        return Filter(root, out status);
                    }
                    status = (int)HttpStatusCode.InternalServerError;
                    Exception? result = null;
                    foreach (var ex in ae2.InnerExceptions)
                    {
                        result = Filter(ex, out status);
                        if (status != (int)HttpStatusCode.InternalServerError)
                        {
                            break;
                        }
                    }
                    return result ?? new InvalidOperationException();
                case ResourceNotFoundException:
                    status = (int)HttpStatusCode.NotFound;
                    break;
                case ResourceInvalidStateException:
                    status = (int)HttpStatusCode.Forbidden;
                    break;
                case ResourceConflictException:
                    status = (int)HttpStatusCode.Conflict;
                    break;
                case SecurityException:
                case UnauthorizedAccessException:
                    status = (int)HttpStatusCode.Unauthorized;
                    break;
                case MethodCallStatusException mcse:
                    status = mcse.Result;
                    break;
                case SerializerException:
                case MethodCallException:
                case BadRequestException:
                case ArgumentException:
                    status = (int)HttpStatusCode.BadRequest;
                    break;
                case NotImplementedException:
                    status = (int)HttpStatusCode.NotImplemented;
                    break;
                case NotSupportedException:
                    status = (int)HttpStatusCode.MethodNotAllowed;
                    break;
                case TimeoutException:
                    status = (int)HttpStatusCode.RequestTimeout;
                    break;
                case SocketException:
                case IOException:
                    status = (int)HttpStatusCode.BadGateway;
                    break;
                case MessageSizeLimitException:
                    status = (int)HttpStatusCode.RequestEntityTooLarge;
                    break;
                case TaskCanceledException:
                case OperationCanceledException:
                    status = (int)HttpStatusCode.Gone;
                    return new OperationCanceledException(
                        "Request was canceled by the client or after timeout.");

                //
                // The following will most certainly be retried by our
                // service client implementations and thus dependent
                // services:
                //
                //      InternalServerError
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
                    status = (int)HttpStatusCode.TooManyRequests;
                    break;
                case ExternalDependencyException:
                    status = (int)HttpStatusCode.ServiceUnavailable;
                    break;
                case ResourceOutOfDateException:
                    status = (int)HttpStatusCode.PreconditionFailed;
                    break;
                default:
                    status = (int)HttpStatusCode.InternalServerError;
                    break;
            }
            return exception;
        }
    }
}
