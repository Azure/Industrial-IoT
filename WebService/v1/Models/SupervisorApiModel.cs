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
    /// Supervisor registration model for webservice api
    /// </summary>
    public class SupervisorApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public SupervisorApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public SupervisorApiModel(SupervisorModel model) {
            Id = model.Id;
            Discovering = model.Discovering;
            Domain = model.Domain;
            OutOfSync = model.OutOfSync;
            Connected = model.Connected;
        }

        /// <summary>
        /// Convert back to service node model
        /// </summary>
        /// <returns></returns>
        public SupervisorModel ToServiceModel() {
            return new SupervisorModel {
                Id = Id,
                Discovering = Discovering,
                OutOfSync = OutOfSync,
                Domain = Domain,
                Connected = Connected
            };
        }

        /// <summary>
        /// Supervisor id
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Domain of supervisor
        /// </summary>
        [JsonProperty(PropertyName = "domain",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string Domain { get; set; }

        /// <summary>
        /// Whether the supervisor is in discovery mode
        /// </summary>
        [JsonProperty(PropertyName = "discovering",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public bool? Discovering { get; set; }

        /// <summary>
        /// Whether the registration is out of sync between
        /// client (edge) and server (service) (default: false).
        /// </summary>
        [JsonProperty(PropertyName = "outOfSync",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public bool? OutOfSync { get; set; }

        /// <summary>
        /// Whether edge is connected on this registration
        /// </summary>
        [JsonProperty(PropertyName = "connected",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public bool? Connected { get; set; }
    }
}
