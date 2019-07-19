// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Trust group registration response model
    /// </summary>
    public sealed class TrustGroupRegistrationResponseApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public TrustGroupRegistrationResponseApiModel() {
        }

        /// <summary>
        /// Create response model
        /// </summary>
        /// <param name="model"></param>
        public TrustGroupRegistrationResponseApiModel(TrustGroupRegistrationResultModel model) {
            Id = model.Id;
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public TrustGroupRegistrationResultModel ToServiceModel() {
            return new TrustGroupRegistrationResultModel {
                Id = Id
            };
        }

        /// <summary>
        /// The id of the trust group
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        [Required]
        public string Id { get; set; }
    }
}
