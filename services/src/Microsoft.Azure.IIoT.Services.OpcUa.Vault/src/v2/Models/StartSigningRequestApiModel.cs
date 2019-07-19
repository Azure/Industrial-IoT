// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Signing request
    /// </summary>
    public sealed class StartSigningRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public StartSigningRequestApiModel() {
        }

        /// <summary>
        /// Create signing request
        /// </summary>
        /// <param name="model"></param>
        public StartSigningRequestApiModel(StartSigningRequestModel model) {
            EntityId = model.EntityId;
            GroupId = model.GroupId;
            CertificateRequest = model.CertificateRequest;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public StartSigningRequestModel ToServiceModel() {
            return new StartSigningRequestModel {
                GroupId = GroupId,
                CertificateRequest = CertificateRequest,
                EntityId = EntityId
            };
        }

        /// <summary>
        /// Id of entity to sign a certificate for
        /// </summary>
        [JsonProperty(PropertyName = "entityId")]
        [Required]
        public string EntityId { get; set; }

        /// <summary>
        /// Certificate group id
        /// </summary>
        [JsonProperty(PropertyName = "groupId")]
        [Required]
        public string GroupId { get; set; }

        /// <summary>
        /// Request
        /// </summary>
        [JsonProperty(PropertyName = "certificateRequest")]
        [Required]
        public JToken CertificateRequest { get; set; }
    }
}
