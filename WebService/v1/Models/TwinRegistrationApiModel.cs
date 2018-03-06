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
    /// Twin registration model for webservice api
    /// </summary>
    public class TwinRegistrationApiModel {
        /// <summary>
        /// Default constructor
        /// </summary>
        public TwinRegistrationApiModel() {}

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public TwinRegistrationApiModel(TwinRegistrationModel model) {
            Id = model.Id;
            Endpoint = new EndpointApiModel(model.Endpoint);
            Server = new ServerInfoApiModel(model.Server);
            OutOfSync = model.OutOfSync;
            Connected = model.Connected;
        }

        /// <summary>
        /// Convert back to service node model
        /// </summary>
        /// <returns></returns>
        public TwinRegistrationModel ToServiceModel() {
            return new TwinRegistrationModel {
                Id = Id,
                Server = Server.ToServiceModel(),
                Endpoint = Endpoint.ToServiceModel(),
                OutOfSync = OutOfSync,
                Connected = Connected
            };
        }

        /// <summary>
        /// Registered identifier of the twin
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Server information of the registration
        /// </summary>
        [JsonProperty(PropertyName = "server")]
        [Required]
        public ServerInfoApiModel Server { get; set; }

        /// <summary>
        /// Endpoint information of the registration
        /// </summary>
        [JsonProperty(PropertyName = "endpoint")]
        [Required]
        public EndpointApiModel Endpoint { get; set; }

        /// <summary>
        /// Whether the registration is out of sync
        /// </summary>
        [JsonProperty(PropertyName = "outOfSync",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null), ReadOnly(true)]
        public bool? OutOfSync { get; set; }

        /// <summary>
        /// Whether edge is connected on this registration
        /// </summary>
        [JsonProperty(PropertyName = "connected",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null), ReadOnly(true)]
        public bool? Connected { get; set; }
    }
}
