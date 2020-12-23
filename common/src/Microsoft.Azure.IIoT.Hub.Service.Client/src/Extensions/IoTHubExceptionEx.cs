// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.Devices.Common.Exceptions {
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Http.Exceptions;
    using System;
    using System.Net;

    /// <summary>
    /// IoT Hub exception extension
    /// </summary>
    public static class IoTHubExceptionEx {

        /// <summary>
        /// Translate exception
        /// </summary>
        public static Exception Rethrow(this Exception ex) {
            switch (ex) {
                case ModuleNotFoundException mex:
                    return new ResourceNotFoundException(mex.Message, mex);
                case DeviceNotFoundException dex:
                    return new ResourceNotFoundException(dex.Message, dex);
                case DeviceAlreadyExistsException aex:
                    return new ConflictingResourceException(aex.Message, aex);
                case ConfigurationNotFoundException cex:
                    return new ResourceNotFoundException(cex.Message, cex);
                case JobNotFoundException jex:
                    return new ResourceNotFoundException(jex.Message, jex);
                case IotHubNotFoundException iex:
                    return new ResourceNotFoundException(iex.Message, iex);
                case UnauthorizedException ue:
                    return new UnauthorizedAccessException(ue.Message, ue);
                case MessageTooLargeException mtl:
                    return new IIoT.Exceptions.MessageTooLargeException(mtl.Message);
                case DeviceMessageLockLostException mtl:
                    return new BadRequestException(mtl.Message, mtl);
                case TooManyModulesOnDeviceException tmd:
                    return new BadRequestException(tmd.Message, tmd);
                case PreconditionFailedException pf:
                    return new ResourceOutOfDateException(pf.Message, pf);
                case ServerErrorException se:
                    return new HttpTransientException(HttpStatusCode.InternalServerError, se.Message);
                case ServerBusyException sb:
                    return new HttpTransientException(HttpStatusCode.ServiceUnavailable, sb.Message);
                case IotHubThrottledException te:
                    return new HttpTransientException((HttpStatusCode)429, te.Message);
                default:
                    return ex;
            }
        }

    }
}
