// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Endpoint Activation status model
    /// </summary>
    public class EndpointActivationStatusApiModel {

        /// <summary>
        /// Identifier of the endoint
        /// </summary>
        [JsonProperty(PropertyName = "id",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        /// <summary>
        /// Activation state
        /// </summary>
        [JsonProperty(PropertyName = "activationState",
            NullValueHandling = NullValueHandling.Ignore)]
        public EndpointActivationState? ActivationState { get; set; }
    }
}
