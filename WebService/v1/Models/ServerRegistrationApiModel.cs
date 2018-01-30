// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.WebService.v1.Models {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Models;
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Server registration model for webservice api
    /// </summary>
    public class ServerRegistrationApiModel {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ServerRegistrationApiModel() {}

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public ServerRegistrationApiModel(ServerRegistrationModel model) {
            Id = model.Id;
            Endpoint = new ServerEndpointApiModel(model.Endpoint);
        }

        /// <summary>
        /// Convert back to service node model
        /// </summary>
        /// <returns></returns>
        public ServerRegistrationModel ToServiceModel() {
            return new ServerRegistrationModel {
                Id = Id,
                Endpoint = Endpoint.ToServiceModel()
            };
        }

        /// <summary>
        /// Registered identifier of the server
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Endpoint information of the server to register
        /// </summary>
        [JsonProperty(PropertyName = "Endpoint")]
        [Required]
        public ServerEndpointApiModel Endpoint { get; set; }
    }
}
