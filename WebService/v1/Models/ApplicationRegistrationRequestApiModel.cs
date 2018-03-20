// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.v1.Models {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Application registration request
    /// </summary>
    public class ApplicationRegistrationRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ApplicationRegistrationRequestApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public ApplicationRegistrationRequestApiModel(ApplicationRegistrationRequestModel model) {
            DiscoveryUrl = model.DiscoveryUrl;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public ApplicationRegistrationRequestModel ToServiceModel() {
            return new ApplicationRegistrationRequestModel {
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
