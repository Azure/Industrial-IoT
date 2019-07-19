// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// New key pair request
    /// </summary>
    public sealed class StartNewKeyPairRequestApiModel {

        /// <summary>
        /// Entity id
        /// </summary>
        [JsonProperty(PropertyName = "entityId")]
        public string EntityId { get; set; }

        /// <summary>
        /// Certificate group
        /// </summary>
        [JsonProperty(PropertyName = "groupId")]
        public string GroupId { get; set; }

        /// <summary>
        /// Type
        /// </summary>
        [JsonProperty(PropertyName = "certificateType")]
        public TrustGroupType CertificateType { get; set; }

        /// <summary>
        /// Subject name
        /// </summary>
        [JsonProperty(PropertyName = "subjectName")]
        public string SubjectName { get; set; }

        /// <summary>
        /// Domain names
        /// </summary>
        [JsonProperty(PropertyName = "domainNames")]
        public List<string> DomainNames { get; set; }
    }
}
