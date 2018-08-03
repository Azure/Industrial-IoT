// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.Api;
using Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Opc.Ua.Gds.Server.GdsVault
{
    public class GdsVaultCertificateRequest : ICertificateRequest
    {
        private Dictionary<NodeId, string> _certTypeMap;

        private IOpcGdsVault _gdsVaultServiceClient { get; }
        public GdsVaultCertificateRequest(IOpcGdsVault gdsVaultServiceClient)
        {
            this._gdsVaultServiceClient = gdsVaultServiceClient;
            this._certTypeMap = new Dictionary<NodeId, string>();

            // list of supported cert type mappings (V1.04)
            this._certTypeMap.Add(Opc.Ua.ObjectTypeIds.HttpsCertificateType, "Https");
            this._certTypeMap.Add(Opc.Ua.ObjectTypeIds.UserCredentialCertificateType, "User");
            this._certTypeMap.Add(Opc.Ua.ObjectTypeIds.ApplicationCertificateType, "App");
            this._certTypeMap.Add(Opc.Ua.ObjectTypeIds.RsaMinApplicationCertificateType, "AppRsaMin");
            this._certTypeMap.Add(Opc.Ua.ObjectTypeIds.RsaSha256ApplicationCertificateType, "AppRsaSha256");
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
            string appId = GdsVaultClientHelper.GetServiceIdFromNodeId(applicationId, NamespaceIndex);
            if (string.IsNullOrEmpty(appId))
            {
                throw new ServiceResultException(StatusCodes.BadInvalidArgument, "The ApplicationId is invalid.");
            }

            string certTypeId;
            if (!_certTypeMap.TryGetValue(certificateTypeId, out certTypeId))
            {
                throw new ServiceResultException(StatusCodes.BadInvalidArgument, "The CertificateTypeId does not refer to a supported CertificateType.");
            }

            var model = new StartSigningRequestApiModel(
                appId,
                authorityId,
                certTypeId,
                Convert.ToBase64String(certificateRequest),
                certificateGroupId.ToString()
                );

            string requestId = _gdsVaultServiceClient.StartSigningRequest(model);

            return GdsVaultClientHelper.GetNodeIdFromServiceId(requestId, NamespaceIndex);
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
            string appId = GdsVaultClientHelper.GetServiceIdFromNodeId(applicationId, NamespaceIndex);
            if (string.IsNullOrEmpty(appId))
            {
                throw new ServiceResultException(StatusCodes.BadInvalidArgument, "The ApplicationId is invalid.");
            }

            string certTypeId;
            if (!_certTypeMap.TryGetValue(certificateTypeId, out certTypeId))
            {
                throw new ServiceResultException(StatusCodes.BadInvalidArgument, "The CertificateTypeId does not refer to a supported CertificateType.");
            }

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

            string requestId = _gdsVaultServiceClient.StartNewKeyPairRequest(model);

            return GdsVaultClientHelper.GetNodeIdFromServiceId(requestId, NamespaceIndex);
        }

        public void ApproveRequest(
            NodeId requestId,
            bool isRejected
            )
        {
            // intentionally ignore the auto approval, it is implemented in the GdsVault service
            string reqId = GdsVaultClientHelper.GetServiceIdFromNodeId(requestId, NamespaceIndex);
            _gdsVaultServiceClient.ApproveCertificateRequest(reqId, isRejected);
        }

        public void AcceptRequest(NodeId requestId, byte[] signedCertificate)
        {
            string reqId = GdsVaultClientHelper.GetServiceIdFromNodeId(requestId, NamespaceIndex);
            _gdsVaultServiceClient.AcceptCertificateRequest(reqId);
        }

        public CertificateRequestState FinishRequest(
            NodeId applicationId,
            NodeId requestId,
            out NodeId certificateGroupId,
            out NodeId certificateTypeId,
            out byte[] signedCertificate,
            out byte[] privateKey)
        {
            string reqId = GdsVaultClientHelper.GetServiceIdFromNodeId(requestId, NamespaceIndex);
            if (string.IsNullOrEmpty(reqId))
            {
                throw new ServiceResultException(StatusCodes.BadInvalidArgument, "The RequestId is invalid.");
            }

            string appId = GdsVaultClientHelper.GetServiceIdFromNodeId(applicationId, NamespaceIndex);
            if (string.IsNullOrEmpty(appId))
            {
                throw new ServiceResultException(StatusCodes.BadInvalidArgument, "The ApplicationId is invalid.");
            }

            certificateGroupId = null;
            certificateTypeId = null;
            signedCertificate = null;
            privateKey = null;

            var request = _gdsVaultServiceClient.FinishRequest(reqId, appId);

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
