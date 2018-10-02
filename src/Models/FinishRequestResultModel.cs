// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------



using Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB.Models;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.Models
{
    public sealed class FinishRequestResultModel
    {
        public CertificateRequestState State { get; set; }

        public string ApplicationId { get; set; }

        public string RequestId { get; set; }

        public string CertificateGroupId { get; set; }

        public string CertificateTypeId { get; set; }

        public byte[] SignedCertificate { get; set; }

        public string PrivateKeyFormat { get; set; }

        public byte[] PrivateKey { get; set; }

        public string AuthorityId { get; set; }

        public FinishRequestResultModel(
            CertificateRequestState state
            )
        {
            this.State = state;
        }

        public FinishRequestResultModel(
            CertificateRequestState state,
            string applicationId,
            string requestId,
            string certificateGroupId,
            string certificateTypeId,
            byte[] signedCertificate,
            string privateKeyFormat,
            byte[] privateKey,
            string authorityId)
        {
            this.State = state;
            this.ApplicationId = applicationId;
            this.CertificateGroupId = certificateGroupId;
            this.CertificateTypeId = certificateTypeId;
            this.SignedCertificate = signedCertificate;
            this.PrivateKeyFormat = privateKeyFormat;
            this.PrivateKey = privateKey;
            this.AuthorityId = authorityId;
        }
    }
}

