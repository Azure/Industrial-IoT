// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.CosmosDB.Models;
using Newtonsoft.Json;
using System;

namespace Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.v1.Models
{
    public sealed class FinishRequestApiModel
    {
        [JsonProperty(PropertyName = "RequestId", Order = 5)]
        public string RequestId { get; set; }

        [JsonProperty(PropertyName = "ApplicationId", Order = 10)]
        public string ApplicationId { get; set; }

        [JsonProperty(PropertyName = "State", Order = 15)]
        public string State { get; set; }

        [JsonProperty(PropertyName = "CertificateGroupId", Order = 20)]
        public string CertificateGroupId { get; set; }

        [JsonProperty(PropertyName = "CertificateTypeId", Order = 30)]
        public string CertificateTypeId { get; set; }

        [JsonProperty(PropertyName = "SignedCertificate", Order = 40)]
        public string SignedCertificate { get; set; }

        [JsonProperty(PropertyName = "PrivateKeyFormat", Order = 50)]
        public string PrivateKeyFormat { get; set; }

        [JsonProperty(PropertyName = "PrivateKey", Order = 60)]
        public string PrivateKey { get; set; }

        [JsonProperty(PropertyName = "AuthorityId", Order = 70)]
        public string AuthorityId { get; set; }

        public FinishRequestApiModel(
            string requestId,
            string applicationId,
            CertificateRequestState state,
            string certificateGroupId,
            string certificateTypeId,
            byte[] signedCertificate,
            string privateKeyFormat,
            byte[] privateKey,
            string authorityId)
        {
            this.RequestId = requestId;
            this.ApplicationId = applicationId;
            this.State = state.ToString();
            this.CertificateGroupId = certificateGroupId;
            this.CertificateTypeId = certificateTypeId;
            this.SignedCertificate = (signedCertificate != null) ? Convert.ToBase64String(signedCertificate) : null;
            this.PrivateKeyFormat = privateKeyFormat;
            this.PrivateKey = (privateKey != null) ? Convert.ToBase64String(privateKey) : null;
            this.AuthorityId = authorityId;
        }

    }
}
