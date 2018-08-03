// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Newtonsoft.Json;
using System;

namespace Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.CosmosDB.Models
{
    public enum CertificateRequestState
    {
        New,
        Approved,
        Rejected,
        Accepted
    }

    [Serializable]
    public class CertificateRequest
    {
        [JsonProperty(PropertyName = "id")]
        public Guid RequestId { get; set; }
        public string ApplicationId { get; set; }
        public CertificateRequestState State { get; set; }
        public string CertificateGroupId { get; set; }
        public string CertificateTypeId { get; set; }
        public byte[] SigningRequest { get; set; }
        public string SubjectName { get; set; }
        public string[] DomainNames { get; set; }
        public string PrivateKeyFormat { get; set; }
        public byte[] PrivateKey { get; set; }
        public string PrivateKeyPassword { get; set; }
        public string AuthorityId { get; set; }
        public byte[] Certificate { get; set; }
        public DateTime RequestTime { get; set; }
        public DateTime ApproveRejectTime { get; set; }
        public DateTime AcceptTime { get; set; }
    }
}
