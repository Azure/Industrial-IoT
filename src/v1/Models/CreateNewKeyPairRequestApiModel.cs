// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models
{
    public sealed class CreateNewKeyPairRequestApiModel
    {
        [JsonProperty(PropertyName = "applicationId", Order = 10)]
        public string ApplicationId { get; set; }

        [JsonProperty(PropertyName = "certificateGroupId", Order = 20)]
        public string CertificateGroupId { get; set; }

        [JsonProperty(PropertyName = "certificateTypeId", Order = 30)]
        public string CertificateTypeId { get; set; }

        [JsonProperty(PropertyName = "subjectName", Order = 40)]
        public string SubjectName { get; set; }

        [JsonProperty(PropertyName = "domainNames", Order = 50)]
        public IList<string> DomainNames { get; set; }

        [JsonProperty(PropertyName = "privateKeyFormat", Order = 60)]
        public string PrivateKeyFormat { get; set; }

        [JsonProperty(PropertyName = "privateKeyPassword", Order = 70)]
        public string PrivateKeyPassword { get; set; }

        public CreateNewKeyPairRequestApiModel(
            string applicationId,
            string certificateGroupId,
            string certificateTypeId,
            string subjectName,
            IList<string> domainNames,
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
