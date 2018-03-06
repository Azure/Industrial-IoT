// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Server with list of endpoints
    /// </summary>
    public class ServerApiModel {

        /// <summary>
        /// Server information
        /// </summary>
        [JsonProperty(PropertyName = "server")]
        public ServerInfoApiModel Server { get; set; }

        /// <summary>
        /// List of endpoints
        /// </summary>
        [JsonProperty(PropertyName = "endpoints",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<EndpointApiModel> Endpoints { get; set; }
    }
}
