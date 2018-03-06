// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.v1.Models {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel;

    /// <summary>
    /// Server model for webservice api
    /// </summary>
    public class ServerInfoApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ServerInfoApiModel() { }

        /// <summary>
        /// Create model from service model
        /// </summary>
        /// <param name="model"></param>
        public ServerInfoApiModel(ServerInfoModel model) {
            ServerId = model.ServerId;
            ApplicationUri = model.ApplicationUri;
            ApplicationName = model.ApplicationName;
            SupervisorId = model.SupervisorId;
            Capabilities = model.Capabilities;
        }

        /// <summary>
        /// Create server model from model
        /// </summary>
        public ServerInfoModel ToServiceModel() {
            return new ServerInfoModel {
                ServerId = ServerId,
                ApplicationUri = ApplicationUri,
                ApplicationName = ApplicationName,
                SupervisorId = SupervisorId,
                Capabilities = Capabilities
            };
        }

        /// <summary>
        /// Unique server id
        /// </summary>
        [JsonProperty(PropertyName = "serverId")]
        public string ServerId { get; set; }

        /// <summary>
        /// Unique application uri
        /// </summary>
        [JsonProperty(PropertyName = "applicationUri")]
        public string ApplicationUri { get; set; }

        /// <summary>
        /// Edge supervisor that validated or found the server
        /// </summary>
        [JsonProperty(PropertyName = "supervisorId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string SupervisorId { get; set; }

        /// <summary>
        /// Name of server
        /// </summary>
        [JsonProperty(PropertyName = "applicationName",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string ApplicationName { get; set; }

        /// <summary>
        /// Capabilities
        /// </summary>
        [JsonProperty(PropertyName = "capabilities",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public List<string> Capabilities { get; set; }
    }
}
