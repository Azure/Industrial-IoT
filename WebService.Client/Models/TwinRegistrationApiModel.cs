// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Twin registration model for webservice api
    /// </summary>
    public class TwinRegistrationApiModel {

        /// <summary>
        /// Registered identifier of the twin
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Server information of the registration
        /// </summary>
        [JsonProperty(PropertyName = "server")]
        public ServerInfoApiModel Server { get; set; }

        /// <summary>
        /// Endpoint information of the registration
        /// </summary>
        [JsonProperty(PropertyName = "endpoint")]
        public EndpointApiModel Endpoint { get; set; }

        /// <summary>
        /// Whether the registration is out of sync between
        /// client (edge) and server (service) (default: false).
        /// </summary>
        [JsonProperty(PropertyName = "outOfSync",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? OutOfSync { get; set; }

        /// <summary>
        /// Whether edge is connected on this registration
        /// </summary>
        [JsonProperty(PropertyName = "connected",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? Connected { get; set; }
    }
}
