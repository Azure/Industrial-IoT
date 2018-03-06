// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.v1.Models {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;

    /// <summary>
    /// Server with list of endpoints
    /// </summary>
    public class ServerApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ServerApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public ServerApiModel(ServerModel model) {
            Server = new ServerInfoApiModel(model.Server);
            if (model.Endpoints != null) {
                Endpoints = model.Endpoints
                    .Select(s => new EndpointApiModel(s))
                    .ToList();
            }
            else {
                Endpoints = new List<EndpointApiModel>();
            }
        }

        /// <summary>
        /// Convert back to service node model
        /// </summary>
        /// <returns></returns>
        public ServerModel ToServiceModel() {
            return new ServerModel {
                Server = Server.ToServiceModel(),
                Endpoints = Endpoints.Count == 0 ? null :
                    Endpoints.Select(e => e.ToServiceModel()).ToList()
            };
        }

        /// <summary>
        /// Server information
        /// </summary>
        [JsonProperty(PropertyName = "server")]
        [Required]
        public ServerInfoApiModel Server { get; set; }

        /// <summary>
        /// List of endpoints
        /// </summary>
        [JsonProperty(PropertyName = "endpoints",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public List<EndpointApiModel> Endpoints { get; set; }
    }
}
