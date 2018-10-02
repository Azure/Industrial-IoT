// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Newtonsoft.Json;
using System;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models
{
    public sealed class StartSigningRequestApiModel
    {
        [JsonProperty(PropertyName = "ApplicationId", Order = 10)]
        public string ApplicationId { get; set; }

        [JsonProperty(PropertyName = "CertificateGroupId", Order = 20)]
        public string CertificateGroupId { get; set; }

        [JsonProperty(PropertyName = "CertificateTypeId", Order = 30)]
        public string CertificateTypeId { get; set; }

        [JsonProperty(PropertyName = "CertificateRequest", Order = 40)]
        public string CertificateRequest { get; set; }

        [JsonProperty(PropertyName = "AuthorityId", Order = 50)]
        public string AuthorityId { get; set; }

        public StartSigningRequestApiModel(
            string applicationId,
            string certificateGroupId,
            string certificateTypeId,
            byte[] certificateRequest,
            string authorityId)
        {
            this.ApplicationId = applicationId;
            this.CertificateGroupId = certificateGroupId;
            this.CertificateTypeId = certificateTypeId;
            this.CertificateRequest = certificateRequest != null ? Convert.ToBase64String(certificateRequest) : null;
            this.AuthorityId = authorityId;
        }

        public byte [] ToServiceModel()
        {
            return CertificateRequest != null ? Convert.FromBase64String(CertificateRequest) : null;
        }

    }
}
