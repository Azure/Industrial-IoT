// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Trust group registration model
    /// </summary>
    public sealed class TrustGroupRegistrationApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public TrustGroupRegistrationApiModel() {
        }

        /// <summary>
        /// Create registration model
        /// </summary>
        /// <param name="model"></param>
        public TrustGroupRegistrationApiModel(TrustGroupRegistrationModel model) {
            Id = model.Id;
            Group = model.Group == null ? null : new TrustGroupApiModel(model.Group);
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public TrustGroupRegistrationModel ToServiceModel() {
            return new TrustGroupRegistrationModel {
                Id = Id,
                Group = Group?.ToServiceModel()
            };
        }

        /// <summary>
        /// The registered id of the trust group
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Trust group
        /// </summary>
        [JsonProperty(PropertyName = "group")]
        [Required]
        public TrustGroupApiModel Group { get; set; }
    }
}
