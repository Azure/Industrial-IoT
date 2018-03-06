// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Supervisor registration model for webservice api
    /// </summary>
    public class SupervisorApiModel {

        /// <summary>
        /// Supervisor id
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Domain of supervisor
        /// </summary>
        [JsonProperty(PropertyName = "domain",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Domain { get; set; }

        /// <summary>
        /// Whether the supervisor is in discovery mode
        /// </summary>
        [JsonProperty(PropertyName = "discovering",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? Discovering { get; set; }

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
