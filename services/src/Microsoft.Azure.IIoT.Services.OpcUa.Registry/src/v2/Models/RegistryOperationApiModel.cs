// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;
    using System;

    /// <summary>
    /// Registry operation log model
    /// </summary>
    public class RegistryOperationApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public RegistryOperationApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public RegistryOperationApiModel(RegistryOperationContextModel model) {
            AuthorityId = model.AuthorityId;
            Time = model.Time;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public RegistryOperationContextModel ToServiceModel() {
            return new RegistryOperationContextModel {
                Time = Time,
                AuthorityId = AuthorityId
            };
        }

        /// <summary>
        /// Operation User
        /// </summary>
        [JsonProperty(PropertyName = "authorityId")]
        [Required]
        public string AuthorityId { get; set; }

        /// <summary>
        /// Operation time
        /// </summary>
        [JsonProperty(PropertyName = "time")]
        [Required]
        public DateTime Time { get; set; }
    }
}

