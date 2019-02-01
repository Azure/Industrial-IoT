// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Newtonsoft.Json;
using System;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models
{
    public sealed class CreateSigningRequestApiModel
    {
        [JsonProperty(PropertyName = "applicationId", Order = 10)]
        public string ApplicationId { get; set; }

        [JsonProperty(PropertyName = "certificateGroupId", Order = 20)]
        public string CertificateGroupId { get; set; }

        [JsonProperty(PropertyName = "certificateTypeId", Order = 30)]
        public string CertificateTypeId { get; set; }

        [JsonProperty(PropertyName = "certificateRequest", Order = 40)]
        public string CertificateRequest { get; set; }

        public CreateSigningRequestApiModel(
            string applicationId,
            string certificateGroupId,
            string certificateTypeId,
            byte[] certificateRequest)
        {
            this.ApplicationId = applicationId;
            this.CertificateGroupId = certificateGroupId;
            this.CertificateTypeId = certificateTypeId;
            this.CertificateRequest = certificateRequest != null ? Convert.ToBase64String(certificateRequest) : null;
        }

        public byte [] ToServiceModel()
        {
            return CertificateRequest != null ? Convert.FromBase64String(CertificateRequest) : null;
        }

    }
}
