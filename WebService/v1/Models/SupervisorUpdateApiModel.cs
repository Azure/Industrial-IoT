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
    /// Supervisor registration update request
    /// </summary>
    public class SupervisorUpdateApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public SupervisorUpdateApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public SupervisorUpdateApiModel(SupervisorUpdateModel model) {
            Id = model.Id;
            Domain = model.Domain;
            Discovering = model.Discovering;
        }

        /// <summary>
        /// Convert back to service node model
        /// </summary>
        /// <returns></returns>
        public SupervisorUpdateModel ToServiceModel() {
            return new SupervisorUpdateModel {
                Id = Id,
                Domain = Domain,
                Discovering = Discovering
            };
        }

        /// <summary>
        /// Identifier of the supervisor to patch
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Domain of supervisor - if null does not change.
        /// empty string to delete.
        /// </summary>
        [JsonProperty(PropertyName = "domain",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string Domain { get; set; }

        /// <summary>
        /// Whether the supervisor is in discovery mode.
        /// If null, does not change.
        /// </summary>
        [JsonProperty(PropertyName = "discovering",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public bool? Discovering { get; set; }
    }
}
