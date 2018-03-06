// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Supervisor registration update request
    /// </summary>
    public class SupervisorUpdateApiModel {

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
    }
}
