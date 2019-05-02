// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Types;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.Models
{
    public sealed class FetchRequestResultModel
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

        public FetchRequestResultModel(
            CertificateRequestState state
            )
        {
            this.State = state;
        }

        public FetchRequestResultModel(
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
            this.RequestId = requestId;
            this.CertificateGroupId = certificateGroupId;
            this.CertificateTypeId = certificateTypeId;
            this.SignedCertificate = signedCertificate;
            this.PrivateKeyFormat = privateKeyFormat;
            this.PrivateKey = privateKey;
            this.AuthorityId = authorityId;
        }
    }
}

