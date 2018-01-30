// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.WebService.v1.Models {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Models;
    using Newtonsoft.Json;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Server registration request
    /// </summary>
    public class ServerRegistrationRequestApiModel {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ServerRegistrationRequestApiModel() {}

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public ServerRegistrationRequestApiModel(ServerRegistrationRequestModel model) {
            Id = model.Id;
            Endpoint = new ServerEndpointApiModel(model.Endpoint);
            Validate = model.Validate;
        }

        /// <summary>
        /// Convert back to service node model
        /// </summary>
        /// <returns></returns>
        public ServerRegistrationRequestModel ToServiceModel() {
            return new ServerRegistrationRequestModel {
                Id = Id,
                Validate = Validate,
                Endpoint = Endpoint.ToServiceModel()
            };
        }

        /// <summary>
        /// Endpoint information of the server to register
        /// </summary>
        [JsonProperty(PropertyName = "endpoint")]
        [Required]
        public ServerEndpointApiModel Endpoint { get; set; }

        /// <summary>
        /// Registered identifier of the server
        /// </summary>
        [JsonProperty(PropertyName = "id",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string Id { get; set; }

        /// <summary>
        /// Registered identifier of the server
        /// </summary>
        [JsonProperty(PropertyName = "validate",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(true)]
        public bool? Validate { get; set; }
    }
}
