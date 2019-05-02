// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models
{
    public sealed class FetchRequestResultApiModel
    {
        [JsonProperty(PropertyName = "requestId", Order = 5)]
        public string RequestId { get; set; }

        [JsonProperty(PropertyName = "applicationId", Order = 10)]
        public string ApplicationId { get; set; }

        [JsonProperty(PropertyName = "state", Order = 15)]
        [Required]
        public CertificateRequestState State { get; set; }

        [JsonProperty(PropertyName = "certificateGroupId", Order = 20)]
        public string CertificateGroupId { get; set; }

        [JsonProperty(PropertyName = "certificateTypeId", Order = 30)]
        public string CertificateTypeId { get; set; }

        [JsonProperty(PropertyName = "signedCertificate", Order = 40)]
        public string SignedCertificate { get; set; }

        [JsonProperty(PropertyName = "privateKeyFormat", Order = 50)]
        public string PrivateKeyFormat { get; set; }

        [JsonProperty(PropertyName = "privateKey", Order = 60)]
        public string PrivateKey { get; set; }

        [JsonProperty(PropertyName = "authorityId", Order = 70)]
        public string AuthorityId { get; set; }

        public FetchRequestResultApiModel(
            string requestId,
            string applicationId,
            Types.CertificateRequestState state,
            string certificateGroupId,
            string certificateTypeId,
            byte[] signedCertificate,
            string privateKeyFormat,
            byte[] privateKey,
            string authorityId)
        {
            this.RequestId = requestId;
            this.ApplicationId = applicationId;
            this.State = (CertificateRequestState)state;
            this.CertificateGroupId = certificateGroupId;
            this.CertificateTypeId = certificateTypeId;
            this.SignedCertificate = (signedCertificate != null) ? Convert.ToBase64String(signedCertificate) : null;
            this.PrivateKeyFormat = privateKeyFormat;
            this.PrivateKey = (privateKey != null) ? Convert.ToBase64String(privateKey) : null;
            this.AuthorityId = authorityId;
        }

    }
}
