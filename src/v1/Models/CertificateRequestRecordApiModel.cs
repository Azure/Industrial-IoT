// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB.Models;
using Newtonsoft.Json;
using System;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models
{
    public sealed class CertificateRequestRecordApiModel
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

        [JsonProperty(PropertyName = "SigningRequest", Order = 35)]
        public string SigningRequest { get; set; }

        [JsonProperty(PropertyName = "SubjectName", Order = 40)]
        public string SubjectName { get; set; }

        [JsonProperty(PropertyName = "DomainNames", Order = 50)]
        public string [] DomainNames { get; set; }

        [JsonProperty(PropertyName = "PrivateKeyFormat", Order = 60)]
        public string PrivateKeyFormat { get; set; }

        public CertificateRequestRecordApiModel(
            string requestId,
            string applicationId,
            CertificateRequestState state,
            string certificateGroupId,
            string certificateTypeId,
            byte[] signingRequest,
            string subjectName,
            string[] domainNames,
            string privateKeyFormat)
        {
            this.RequestId = requestId;
            this.ApplicationId = applicationId;
            this.State = state.ToString();
            this.CertificateGroupId = certificateGroupId;
            this.CertificateTypeId = certificateTypeId;
            this.SigningRequest = (signingRequest != null) ? Convert.ToBase64String(signingRequest) : null; 
            this.SubjectName = subjectName;
            this.DomainNames = domainNames;
            this.PrivateKeyFormat = privateKeyFormat;
        }

    }
}
