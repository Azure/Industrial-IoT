// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.Discovery.Filters {
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Exceptions;
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Security;
    using System.Threading.Tasks;

    /// <summary>
    /// Convert all the exceptions returned by the module controllers to a
    /// status code.
    /// </summary>
    public class ExceptionsFilterAttribute : ExceptionFilterAttribute {

        /// <inheritdoc />
        public override Exception Filter(Exception exception, out int status) {
            switch (exception) {
                case AggregateException ae:
                    var root = ae.GetBaseException();
                    if (!(root is AggregateException)) {
                        return Filter(root, out status);
                    }
                    ae = root as AggregateException;
                    status = (int)HttpStatusCode.InternalServerError;
                    Exception result = null;
                    foreach (var ex in ae.InnerExceptions) {
                        result = Filter(ex, out status);
                        if (status != (int)HttpStatusCode.InternalServerError) {
                            break;
                        }
                    }
                    return result;
                case ResourceNotFoundException re:
                    status = (int)HttpStatusCode.NotFound;
                    break;
                case ResourceInvalidStateException ri:
                    status = (int)HttpStatusCode.Forbidden;
                    break;
                case ConflictingResourceException ce:
                    status = (int)HttpStatusCode.Conflict;
                    break;
                case SecurityException se:
                case UnauthorizedAccessException ue:
                    status = (int)HttpStatusCode.Unauthorized;
                    break;
                case SerializerException sre:
                case MethodCallException mce:
                case BadRequestException br:
                case ArgumentException are:
                    status = (int)HttpStatusCode.BadRequest;
                    break;
                case NotImplementedException ne:
                case NotSupportedException ns:
                    status = (int)HttpStatusCode.NotImplemented;
                    break;
                case TimeoutException te:
                    status = (int)HttpStatusCode.RequestTimeout;
                    break;
                case SocketException sex:
                case CommunicationException ce:
                    status = (int)HttpStatusCode.BadGateway;
                    break;
                case MessageTooLargeException mtl:
                    status = (int)HttpStatusCode.RequestEntityTooLarge;
                    break;
                case TaskCanceledException tce:
                case OperationCanceledException oce:
                    status = (int)HttpStatusCode.Gone;
                    break;

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

                case ServerBusyException se:
                    status = (int)HttpStatusCode.TooManyRequests;
                    break;
                case ExternalDependencyException ex:
                    status = (int)HttpStatusCode.ServiceUnavailable;
                    break;
                case ResourceOutOfDateException re:
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
