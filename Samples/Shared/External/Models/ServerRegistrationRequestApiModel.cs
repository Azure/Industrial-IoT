// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.Shared.External.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Server registration request
    /// </summary>
    public class ServerRegistrationRequestApiModel {

        /// <summary>
        /// Endpoint information of the server to register
        /// </summary>
        [JsonProperty(PropertyName = "endpoint")]
        public ServerEndpointApiModel Endpoint { get; set; }

        /// <summary>
        /// Registered identifier of the server
        /// </summary>
        [JsonProperty(PropertyName = "id",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        /// <summary>
        /// Registered identifier of the server
        /// </summary>
        [JsonProperty(PropertyName = "validate",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? Validate { get; set; }
    }
}
