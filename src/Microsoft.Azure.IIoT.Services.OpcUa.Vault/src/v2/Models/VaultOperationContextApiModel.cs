// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Newtonsoft.Json;
    using System;
    using System.ComponentModel.DataAnnotations;


    /// <summary>
    /// Vault operation log model
    /// </summary>
    public class VaultOperationContextApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public VaultOperationContextApiModel() {
        }

        /// <summary>
        /// Create new context
        /// </summary>
        /// <param name="model"></param>
        public VaultOperationContextApiModel(VaultOperationContextModel model) {
            Time = model.Time;
            AuthorityId = model.AuthorityId;
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        public VaultOperationContextModel ToServiceModel() {
            return new VaultOperationContextModel {
                Time = Time,
                AuthorityId = AuthorityId,
            };
        }

        /// <summary>
        /// User
        /// </summary>
        [JsonProperty(PropertyName = "authorityId")]
        public string AuthorityId { get; set; }

        /// <summary>
        /// Operation time
        /// </summary>
        [JsonProperty(PropertyName = "time")]
        [Required]
        public DateTime Time { get; set; }
    }
}

