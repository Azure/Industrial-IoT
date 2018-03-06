// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Server model for webservice api
    /// </summary>
    public class ServerInfoApiModel {

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
        public string ApplicationName { get; set; }

        /// <summary>
        /// Capabilities
        /// </summary>
        [JsonProperty(PropertyName = "capabilities",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Capabilities { get; set; }
    }
}
