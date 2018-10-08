// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Azure.IIoT.OpcUa.Api.Vault;
using Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Opc.Ua.Gds.Server.OpcVault
{
    public class OpcVaultCertificateRequest : ICertificateRequest
    {
        private Dictionary<NodeId, string> _certTypeMap;

        private IOpcVault _opcVaultServiceClient { get; }
        public OpcVaultCertificateRequest(IOpcVault opcVaultServiceClient)
        {
            this._opcVaultServiceClient = opcVaultServiceClient;
            this._certTypeMap = new Dictionary<NodeId, string>();

            // list of supported cert type mappings (V1.04)
            this._certTypeMap.Add(Opc.Ua.ObjectTypeIds.HttpsCertificateType, "HttpsCertificateType");
            this._certTypeMap.Add(Opc.Ua.ObjectTypeIds.UserCredentialCertificateType, "UserCredentialCertificateType");
            this._certTypeMap.Add(Opc.Ua.ObjectTypeIds.ApplicationCertificateType, "ApplicationCertificateType");
            this._certTypeMap.Add(Opc.Ua.ObjectTypeIds.RsaMinApplicationCertificateType, "RsaMinApplicationCertificateType");
            this._certTypeMap.Add(Opc.Ua.ObjectTypeIds.RsaSha256ApplicationCertificateType, "RsaSha256ApplicationCertificateType");
        }

        #region ICertificateRequest
        public void Initialize()
        {
        }

        public ushort NamespaceIndex { get; set; }

        public NodeId StartSigningRequest(
            NodeId applicationId,
            NodeId certificateGroupId,
            NodeId certificateTypeId,
            byte[] certificateRequest,
            string authorityId)
        {
            string appId = OpcVaultClientHelper.GetServiceIdFromNodeId(applicationId, NamespaceIndex);
            if (string.IsNullOrEmpty(appId))
            {
                throw new ServiceResultException(StatusCodes.BadNotFound, "The ApplicationId is invalid.");
            }

            string certTypeId;
            if (!_certTypeMap.TryGetValue(certificateTypeId, out certTypeId))
            {
                throw new ServiceResultException(StatusCodes.BadInvalidArgument, "The CertificateTypeId does not refer to a supported CertificateType.");
            }

            try
            {
                var model = new StartSigningRequestApiModel(
                    appId,
                    authorityId,
                    certTypeId,
                    Convert.ToBase64String(certificateRequest),
                    certificateGroupId.ToString()
                    );

                string requestId = _opcVaultServiceClient.StartSigningRequest(model);
                return OpcVaultClientHelper.GetNodeIdFromServiceId(requestId, NamespaceIndex);
            }
            catch (HttpOperationException httpEx)
            {
                // TODO: return matching ServiceResultException
                //throw new ServiceResultException(StatusCodes.BadNotFound);
                //throw new ServiceResultException(StatusCodes.BadInvalidArgument);
                //throw new ServiceResultException(StatusCodes.BadUserAccessDenied);
                //throw new ServiceResultException(StatusCodes.BadRequestNotAllowed);
                //throw new ServiceResultException(StatusCodes.BadCertificateUriInvalid);
                throw new ServiceResultException(httpEx, StatusCodes.BadNotSupported);
            }
        }

        public NodeId StartNewKeyPairRequest(
            NodeId applicationId,
            NodeId certificateGroupId,
            NodeId certificateTypeId,
            string subjectName,
            string[] domainNames,
            string privateKeyFormat,
            string privateKeyPassword,
            string authorityId)
        {
            string appId = OpcVaultClientHelper.GetServiceIdFromNodeId(applicationId, NamespaceIndex);
            if (string.IsNullOrEmpty(appId))
            {
                throw new ServiceResultException(StatusCodes.BadInvalidArgument, "The ApplicationId is invalid.");
            }

            string certTypeId;
            if (!_certTypeMap.TryGetValue(certificateTypeId, out certTypeId))
            {
                throw new ServiceResultException(StatusCodes.BadInvalidArgument, "The CertificateTypeId does not refer to a supported CertificateType.");
            }
            try
            {
                var model = new StartNewKeyPairRequestApiModel(
                    appId,
                    authorityId,
                    certTypeId,
                    subjectName,
                    domainNames,
                    privateKeyFormat,
                    privateKeyPassword,
                    certificateGroupId.ToString()
                    );

                string requestId = _opcVaultServiceClient.StartNewKeyPairRequest(model);

                return OpcVaultClientHelper.GetNodeIdFromServiceId(requestId, NamespaceIndex);
            }
            catch (HttpOperationException httpEx)
            {
                // TODO: return matching ServiceResultException
                //throw new ServiceResultException(StatusCodes.BadNodeIdUnknown);
                //throw new ServiceResultException(StatusCodes.BadInvalidArgument);
                //throw new ServiceResultException(StatusCodes.BadUserAccessDenied);
                throw new ServiceResultException(httpEx, StatusCodes.BadRequestNotAllowed);
            }

        }

        public void ApproveRequest(
            NodeId requestId,
            bool isRejected
            )
        {
            try
            {
                // intentionally ignore the auto approval, it is implemented in the OpcVault service
                string reqId = OpcVaultClientHelper.GetServiceIdFromNodeId(requestId, NamespaceIndex);
                _opcVaultServiceClient.ApproveCertificateRequest(reqId, isRejected);
            }
            catch (HttpOperationException httpEx)
            {
                throw new ServiceResultException(httpEx, StatusCodes.BadUserAccessDenied);
            }
        }

        public void AcceptRequest(NodeId requestId, byte[] signedCertificate)
        {
            try
            {
                string reqId = OpcVaultClientHelper.GetServiceIdFromNodeId(requestId, NamespaceIndex);
                _opcVaultServiceClient.AcceptCertificateRequest(reqId);
            }
            catch (HttpOperationException httpEx)
            {
                throw new ServiceResultException(httpEx, StatusCodes.BadUserAccessDenied);
            }

        }

        public CertificateRequestState FinishRequest(
            NodeId applicationId,
            NodeId requestId,
            out NodeId certificateGroupId,
            out NodeId certificateTypeId,
            out byte[] signedCertificate,
            out byte[] privateKey)
        {
            string reqId = OpcVaultClientHelper.GetServiceIdFromNodeId(requestId, NamespaceIndex);
            if (string.IsNullOrEmpty(reqId))
            {
                throw new ServiceResultException(StatusCodes.BadInvalidArgument, "The RequestId is invalid.");
            }

            string appId = OpcVaultClientHelper.GetServiceIdFromNodeId(applicationId, NamespaceIndex);
            if (string.IsNullOrEmpty(appId))
            {
                throw new ServiceResultException(StatusCodes.BadInvalidArgument, "The ApplicationId is invalid.");
            }

            certificateGroupId = null;
            certificateTypeId = null;
            signedCertificate = null;
            privateKey = null;
            try
            {
                var request = _opcVaultServiceClient.FinishRequest(reqId, appId);

                var state = (CertificateRequestState)Enum.Parse(typeof(CertificateRequestState), request.State);

                if (state == CertificateRequestState.Approved)
                {
                    certificateGroupId = new NodeId(request.AuthorityId);
                    certificateTypeId = _certTypeMap.FirstOrDefault(x => x.Value == request.CertificateTypeId).Key;
                    signedCertificate = request.SignedCertificate != null ? Convert.FromBase64String(request.SignedCertificate) : null;
                    privateKey = request.PrivateKey != null ? Convert.FromBase64String(request.PrivateKey) : null;
                }
                return state;
            }
            catch (HttpOperationException httpEx)
            {
                //throw new ServiceResultException(StatusCodes.BadNotFound);
                //throw new ServiceResultException(StatusCodes.BadInvalidArgument);
                //throw new ServiceResultException(StatusCodes.BadUserAccessDenied);
                //throw new ServiceResultException(StatusCodes.BadNothingToDo);
                throw new ServiceResultException(httpEx, StatusCodes.BadRequestNotAllowed);
            }
        }

        public CertificateRequestState ReadRequest(
            NodeId applicationId,
            NodeId requestId,
            out NodeId certificateGroupId,
            out NodeId certificateTypeId,
            out byte[] certificateRequest,
            out string subjectName,
            out string[] domainNames,
            out string privateKeyFormat,
            out string privateKeyPassword)
        {
            throw new NotImplementedException();
        }
#endregion
    }
}
