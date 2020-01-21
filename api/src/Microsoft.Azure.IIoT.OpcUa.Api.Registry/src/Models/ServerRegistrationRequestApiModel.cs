// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Server registration request
    /// </summary>
    public class ServerRegistrationRequestApiModel {


        /// <summary>
        /// Discovery url to use for registration
        /// </summary>
        [JsonProperty(PropertyName = "discoveryUrl")]
        [Required]
        public string DiscoveryUrl { get; set; }

        /// <summary>
        /// Registration id
        /// </summary>
        [JsonProperty(PropertyName = "id",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string Id { get; set; }

        /// <summary>
        /// Upon discovery, activate all endpoints with this filter.
        /// </summary>
        [JsonProperty(PropertyName = "activationFilter",
           NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public EndpointActivationFilterApiModel ActivationFilter { get; set; }
    }
}
