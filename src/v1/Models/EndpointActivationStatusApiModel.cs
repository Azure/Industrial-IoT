// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Endpoint Activation status model
    /// </summary>
    public class EndpointActivationStatusApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public EndpointActivationStatusApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public EndpointActivationStatusApiModel(EndpointActivationStatusModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Id = model.Id;
            ActivationState = model.ActivationState;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public EndpointActivationStatusModel ToServiceModel() {
            return new EndpointActivationStatusModel {
                Id = Id,
                ActivationState = ActivationState
            };
        }

        /// <summary>
        /// Identifier of the endoint
        /// </summary>
        [JsonProperty(PropertyName = "Id")]
        public string Id { get; set; }

        /// <summary>
        /// Activation state
        /// </summary>
        [JsonProperty(PropertyName = "ActivationState",
            NullValueHandling = NullValueHandling.Ignore)]
        public EndpointActivationState? ActivationState { get; set; }
    }
}
