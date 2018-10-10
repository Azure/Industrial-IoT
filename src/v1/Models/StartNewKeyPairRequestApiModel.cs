// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Newtonsoft.Json;
using System;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models
{
    public sealed class StartNewKeyPairRequestApiModel
    {
        [JsonProperty(PropertyName = "ApplicationId", Order = 10)]
        public string ApplicationId { get; set; }

        [JsonProperty(PropertyName = "CertificateGroupId", Order = 20)]
        public string CertificateGroupId { get; set; }

        [JsonProperty(PropertyName = "CertificateTypeId", Order = 30)]
        public string CertificateTypeId { get; set; }

        [JsonProperty(PropertyName = "SubjectName", Order = 40)]
        public string SubjectName { get; set; }

        [JsonProperty(PropertyName = "DomainNames", Order = 50)]
        public string [] DomainNames { get; set; }

        [JsonProperty(PropertyName = "PrivateKeyFormat", Order = 60)]
        public string PrivateKeyFormat { get; set; }

        [JsonProperty(PropertyName = "PrivateKeyPassword", Order = 70)]
        public string PrivateKeyPassword { get; set; }

        public StartNewKeyPairRequestApiModel(
            string applicationId,
            string certificateGroupId,
            string certificateTypeId,
            string subjectName,
            string[] domainNames,
            string privateKeyFormat,
            string privateKeyPassword)
        {
            this.ApplicationId = applicationId;
            this.CertificateGroupId = certificateGroupId;
            this.CertificateTypeId = certificateTypeId;
            this.SubjectName = subjectName;
            this.DomainNames = domainNames;
            this.PrivateKeyFormat = privateKeyFormat;
            this.PrivateKeyPassword = privateKeyPassword;
        }

    }
}
