// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Twin registration request
    /// </summary>
    public class TwinRegistrationRequestApiModel {

        /// <summary>
        /// Endpoint to register
        /// </summary>
        [JsonProperty(PropertyName = "endpoint")]
        public EndpointApiModel Endpoint { get; set; }

        /// <summary>
        /// Desired identifier of the twin
        /// </summary>
        [JsonProperty(PropertyName = "id",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }
    }
}
