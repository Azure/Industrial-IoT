// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Exceptions;
    using Microsoft.Azure.IIoT.Exceptions;
    using Opc.Ua;
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
                case StatusCodes.BadNoContinuationPoints:
                case StatusCodes.BadLicenseLimitsExceeded:
                case StatusCodes.BadTcpServerTooBusy:
                case StatusCodes.BadTooManySessions:
                case StatusCodes.BadTooManyOperations:
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
                    return new CommunicationException(sre.SymbolicId, sre);
                case StatusCodes.BadDisconnect:
                case StatusCodes.BadServerHalted:
                case StatusCodes.BadShutdown:
                case StatusCodes.BadServerNotConnected:
                    return new CommunicationException(sre.SymbolicId, sre);
                case StatusCodes.BadTimeout:
                case StatusCodes.BadRequestTimeout:
                    return new TimeoutException(sre.SymbolicId, sre);
                case StatusCodes.BadWriteNotSupported:
                case StatusCodes.BadHistoryOperationUnsupported:
                case StatusCodes.BadNotSupported:
                case StatusCodes.BadDataEncodingUnsupported:
                case StatusCodes.BadServiceUnsupported:
                    return new NotSupportedException(sre.SymbolicId, sre);
                case StatusCodes.BadNodeNotInView:
                case StatusCodes.BadBoundNotFound:
                case StatusCodes.BadNoDataAvailable:
                case StatusCodes.BadNoData:
                    return new ResourceNotFoundException(sre.SymbolicId, sre);
                case StatusCodes.BadNotReadable:
                case StatusCodes.BadNotWritable:
                case StatusCodes.BadEntryExists:
                case StatusCodes.BadNoEntryExists:
                    return new InvalidOperationException(sre.SymbolicId, sre);
                case StatusCodes.BadOutOfRange:
                    return new IndexOutOfRangeException(sre.SymbolicId, sre);
                case StatusCodes.BadTypeMismatch:
                case StatusCodes.BadArgumentsMissing:
                case StatusCodes.BadInvalidArgument:
                case StatusCodes.BadTooManyArguments:
                case StatusCodes.BadContinuationPointInvalid:
                case StatusCodes.BadNodeIdUnknown:
                case StatusCodes.BadNodeIdInvalid:
                case StatusCodes.BadTimestampsToReturnInvalid:
                case StatusCodes.BadEventIdUnknown:
                case StatusCodes.BadViewIdUnknown:
                case StatusCodes.BadMethodInvalid:
                case StatusCodes.BadAttributeIdInvalid:
                case StatusCodes.BadFilterNotAllowed:
                case StatusCodes.BadAggregateNotSupported:
                case StatusCodes.BadAggregateListMismatch:
                case StatusCodes.BadIndexRangeInvalid:
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
                case StatusCodes.BadUserSignatureInvalid:
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
                case StatusCodes.BadNotImplemented:
                    return new NotImplementedException(sre.SymbolicId, sre);
                default:
                    return new BadRequestException(sre.SymbolicId, sre);
            }
        }
    }
}
