// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// New key pair request
    /// </summary>
    public sealed class StartNewKeyPairRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public StartNewKeyPairRequestApiModel() {
        }

        /// <summary>
        /// Create new request
        /// </summary>
        /// <param name="model"></param>
        public StartNewKeyPairRequestApiModel(StartNewKeyPairRequestModel model) {
            EntityId = model.EntityId;
            GroupId = model.GroupId;
            SubjectName = model.SubjectName;
            DomainNames = model.DomainNames;
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        public StartNewKeyPairRequestModel ToServiceModel() {
            return new StartNewKeyPairRequestModel {
                EntityId = EntityId,
                GroupId = GroupId,
                SubjectName = SubjectName,
                DomainNames = DomainNames,
            };
        }

        /// <summary>
        /// Entity id
        /// </summary>
        [JsonProperty(PropertyName = "entityId")]
        [Required]
        public string EntityId { get; set; }

        /// <summary>
        /// Certificate group
        /// </summary>
        [JsonProperty(PropertyName = "groupId")]
        [Required]
        public string GroupId { get; set; }

        /// <summary>
        /// Type
        /// </summary>
        [JsonProperty(PropertyName = "certificateType")]
        [Required]
        public TrustGroupType CertificateType { get; set; }

        /// <summary>
        /// Subject name
        /// </summary>
        [JsonProperty(PropertyName = "subjectName")]
        [Required]
        public string SubjectName { get; set; }

        /// <summary>
        /// Domain names
        /// </summary>
        [JsonProperty(PropertyName = "domainNames")]
        public List<string> DomainNames { get; set; }
    }
}
