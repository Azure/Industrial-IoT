// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Registry.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Application registration request
    /// </summary>
    public class ServerRegistrationRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ServerRegistrationRequestApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public ServerRegistrationRequestApiModel(ServerRegistrationRequestModel model) {
            DiscoveryUrl = model.DiscoveryUrl;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public ServerRegistrationRequestModel ToServiceModel() {
            return new ServerRegistrationRequestModel {
                DiscoveryUrl = DiscoveryUrl
            };
        }

        /// <summary>
        /// Discovery url to use for registration
        /// </summary>
        [JsonProperty(PropertyName = "discoveryUrl")]
        [Required]
        public string DiscoveryUrl { get; set; }
    }
}
