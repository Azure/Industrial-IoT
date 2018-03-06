// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.v1.Models {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using Newtonsoft.Json;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Twin registration request
    /// </summary>
    public class TwinRegistrationRequestApiModel {
        /// <summary>
        /// Default constructor
        /// </summary>
        public TwinRegistrationRequestApiModel() {}

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public TwinRegistrationRequestApiModel(TwinRegistrationRequestModel model) {
            Id = model.Id;
            Endpoint = new EndpointApiModel(model.Endpoint);
        }

        /// <summary>
        /// Convert back to service node model
        /// </summary>
        /// <returns></returns>
        public TwinRegistrationRequestModel ToServiceModel() {
            return new TwinRegistrationRequestModel {
                Id = Id,
                Endpoint = Endpoint.ToServiceModel()
            };
        }

        /// <summary>
        /// Endpoint information to register
        /// </summary>
        [JsonProperty(PropertyName = "endpoint")]
        [Required]
        public EndpointApiModel Endpoint { get; set; }

        /// <summary>
        /// Desired identifier of the twin
        /// </summary>
        [JsonProperty(PropertyName = "id",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string Id { get; set; }
    }
}
