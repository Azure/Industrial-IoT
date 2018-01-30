// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.Shared.External.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Server registration model for webservice api
    /// </summary>
    public class ServerRegistrationApiModel {

        /// <summary>
        /// Registered identifier of the server
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Endpoint information of the server to register
        /// </summary>
        [JsonProperty(PropertyName = "Endpoint")]
        public ServerEndpointApiModel Endpoint { get; set; }
    }
}
