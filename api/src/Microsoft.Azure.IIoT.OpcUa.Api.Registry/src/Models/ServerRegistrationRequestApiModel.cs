// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Server registration request
    /// </summary>
    public class ServerRegistrationRequestApiModel {

        /// <summary>
        /// Discovery url to use for registration
        /// </summary>
        [JsonProperty(PropertyName = "discoveryUrl")]
        public string DiscoveryUrl { get; set; }

        /// <summary>
        /// Registration id
        /// </summary>
        [JsonProperty(PropertyName = "id",
            NullValueHandling = NullValueHandling.Ignore)]
        public string RegistrationId { get; set; }

        /// <summary>
        /// Callback
        /// </summary>
        [JsonProperty(PropertyName = "callback",
            NullValueHandling = NullValueHandling.Ignore)]
        public CallbackApiModel Callback { get; private set; }

        /// <summary>
        /// Upon discovery, activate all endpoints with this filter.
        /// </summary>
        [JsonProperty(PropertyName = "activationFilter",
           NullValueHandling = NullValueHandling.Ignore)]
        public EndpointActivationFilterApiModel ActivationFilter { get; set; }
    }
}
