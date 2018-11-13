// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua {
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Exceptions;
    using System;

    /// <summary>
    /// Status code extensions
    /// </summary>
    public static class ServiceResultEx {

        /// <summary>
        /// Convert service result exception to typed exception
        /// </summary>
        /// <param name="sre"></param>
        /// <returns></returns>
        public static Exception ToTypedException(this ServiceResultException sre) {
            switch (sre.StatusCode) {
                case StatusCodes.BadProtocolVersionUnsupported:
                case StatusCodes.BadConnectionClosed:
                case StatusCodes.BadNotConnected:
                case StatusCodes.BadTcpEndpointUrlInvalid:
                case StatusCodes.BadConnectionRejected:
                case StatusCodes.BadSecurityModeRejected:
                case StatusCodes.BadSecurityPolicyRejected:
                    return new ConnectionException(sre.SymbolicId, sre);
                case StatusCodes.BadLicenseLimitsExceeded:
                case StatusCodes.BadTcpServerTooBusy:
                case StatusCodes.BadTooManySessions:
                    return new ServerBusyException(sre.SymbolicId, sre);
                case StatusCodes.BadTcpMessageTypeInvalid:
                case StatusCodes.BadTcpMessageTooLarge:
                case StatusCodes.BadSequenceNumberUnknown:
                case StatusCodes.BadSequenceNumberInvalid:
                case StatusCodes.BadNonceInvalid:
                    return new ProtocolException(sre.SymbolicId, sre);
                case StatusCodes.BadSecureChannelClosed:
                case StatusCodes.BadSecureChannelTokenUnknown:
                case StatusCodes.BadSecureChannelIdInvalid:
                case StatusCodes.BadCommunicationError:
                case StatusCodes.BadTcpNotEnoughResources:
                case StatusCodes.BadTcpInternalError:
                case StatusCodes.BadSessionClosed:
                case StatusCodes.BadSessionIdInvalid:
                case StatusCodes.BadDisconnect:
                    return new CommunicationException(sre.SymbolicId, sre);
                case StatusCodes.BadTimeout:
                case StatusCodes.BadRequestTimeout:
                    return new TimeoutException(sre.SymbolicId, sre);
                case StatusCodes.BadWriteNotSupported:
                case StatusCodes.BadMethodInvalid:
                case StatusCodes.BadNotReadable:
                case StatusCodes.BadNotWritable:
                    return new InvalidOperationException(sre.SymbolicId, sre);
                case StatusCodes.BadTypeMismatch:
                case StatusCodes.BadArgumentsMissing:
                case StatusCodes.BadInvalidArgument:
                case StatusCodes.BadTooManyArguments:
                case StatusCodes.BadOutOfRange:
                    return new ArgumentException(sre.SymbolicId, sre);
                case StatusCodes.BadCertificateRevocationUnknown:
                case StatusCodes.BadCertificateIssuerRevocationUnknown:
                case StatusCodes.BadCertificateRevoked:
                case StatusCodes.BadCertificateIssuerRevoked:
                case StatusCodes.BadCertificateChainIncomplete:
                case StatusCodes.BadCertificateIssuerUseNotAllowed:
                case StatusCodes.BadCertificateUseNotAllowed:
                case StatusCodes.BadCertificateUriInvalid:
                case StatusCodes.BadCertificateTimeInvalid:
                case StatusCodes.BadCertificateIssuerTimeInvalid:
                case StatusCodes.BadCertificateInvalid:
                case StatusCodes.BadCertificateHostNameInvalid:
                case StatusCodes.BadNoValidCertificates:
                    return new CertificateInvalidException(sre.SymbolicId, sre);
                case StatusCodes.BadCertificateUntrusted:
                    return new CertificateUntrustedException(sre.SymbolicId, sre);
                case StatusCodes.BadUserAccessDenied:
                case StatusCodes.BadIdentityTokenInvalid:
                case StatusCodes.BadIdentityTokenRejected:
                case StatusCodes.BadRequestNotAllowed:
                case StatusCodes.BadLicenseExpired:
                case StatusCodes.BadLicenseNotAvailable:
                    return new UnauthorizedAccessException(sre.SymbolicId, sre);
                case StatusCodes.BadEncodingError:
                case StatusCodes.BadDecodingError:
                case StatusCodes.BadEncodingLimitsExceeded:
                case StatusCodes.BadRequestTooLarge:
                case StatusCodes.BadResponseTooLarge:
                case StatusCodes.BadDataEncodingInvalid:
                    return new FormatException(sre.SymbolicId, sre);
                case StatusCodes.BadDataEncodingUnsupported:
                case StatusCodes.BadServiceUnsupported:
                case StatusCodes.BadNotSupported:
                    return new NotSupportedException(sre.SymbolicId, sre);
                case StatusCodes.BadNotImplemented:
                    return new NotImplementedException(sre.SymbolicId, sre);
                default:
                    return new BadRequestException(sre.SymbolicId, sre);
            }
        }
    }
}
