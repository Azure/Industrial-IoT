// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System.ComponentModel;

    /// <summary>
    /// Endpoint registration update request
    /// </summary>
    public class EndpointRegistrationUpdateApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public EndpointRegistrationUpdateApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public EndpointRegistrationUpdateApiModel(EndpointRegistrationUpdateModel model) {
            User = model.User == null ?
                null : new CredentialApiModel(model.User);
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public EndpointRegistrationUpdateModel ToServiceModel() {
            return new EndpointRegistrationUpdateModel {
                User = User?.ToServiceModel()
            };
        }

        /// <summary>
        /// User authentication to change on the endpoint.
        /// </summary>
        [JsonProperty(PropertyName = "user",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public CredentialApiModel User { get; set; }
    }
}
