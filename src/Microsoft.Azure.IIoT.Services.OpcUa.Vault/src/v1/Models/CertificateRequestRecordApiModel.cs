// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models
{
    public sealed class CertificateRequestRecordApiModel
    {
        [JsonProperty(PropertyName = "requestId", Order = 5)]
        public string RequestId { get; set; }

        [JsonProperty(PropertyName = "applicationId", Order = 10)]
        public string ApplicationId { get; set; }

        [JsonProperty(PropertyName = "state", Order = 15)]
        [Required]
        public CertificateRequestState State { get; }

        [JsonProperty(PropertyName = "certificateGroupId", Order = 20)]
        public string CertificateGroupId { get; set; }

        [JsonProperty(PropertyName = "certificateTypeId", Order = 30)]
        public string CertificateTypeId { get; set; }

        [JsonProperty(PropertyName = "signingRequest", Order = 35)]
        [Required]
        public bool SigningRequest { get; }

        [JsonProperty(PropertyName = "subjectName", Order = 40)]
        public string SubjectName { get; set; }

        [JsonProperty(PropertyName = "domainNames", Order = 50)]
        public IList<string> DomainNames { get; set; }

        [JsonProperty(PropertyName = "privateKeyFormat", Order = 60)]
        public string PrivateKeyFormat { get; set; }

        public CertificateRequestRecordApiModel(
            string requestId,
            string applicationId,
            Types.CertificateRequestState state,
            string certificateGroupId,
            string certificateTypeId,
            bool signingRequest,
            string subjectName,
            IList<string> domainNames,
            string privateKeyFormat)
        {
            this.RequestId = requestId;
            this.ApplicationId = applicationId;
            this.State = (CertificateRequestState)state;
            this.CertificateGroupId = certificateGroupId;
            this.CertificateTypeId = certificateTypeId;
            this.SigningRequest = signingRequest;
            this.SubjectName = subjectName;
            this.DomainNames = domainNames;
            this.PrivateKeyFormat = privateKeyFormat;
        }

    }
}
