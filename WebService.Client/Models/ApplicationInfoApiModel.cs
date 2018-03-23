// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System.Collections.Generic;

    /// <summary>
    /// Application type
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ApplicationType {
        Server,
        Client,
        ClientServer
    }

    /// <summary>
    /// Application info model for webservice api
    /// </summary>
    public class ApplicationInfoApiModel {

        /// <summary>
        /// Unique server id
        /// </summary>
        [JsonProperty(PropertyName = "applicationId")]
        public string ApplicationId { get; set; }

        /// <summary>
        /// Type of application
        /// </summary>
        [JsonProperty(PropertyName = "applicationType")]
        public ApplicationType ApplicationType { get; set; }

        /// <summary>
        /// Unique application uri
        /// </summary>
        [JsonProperty(PropertyName = "applicationUri")]
        public string ApplicationUri { get; set; }

        /// <summary>
        /// Product uri
        /// </summary>
        [JsonProperty(PropertyName = "productUri",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ProductUri { get; set; }

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

        /// <summary>
        /// Discovery urls of the server
        /// </summary>
        [JsonProperty(PropertyName = "discoveryUrls",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<string> DiscoveryUrls { get; set; }

        /// <summary>
        /// Discovery profile uri
        /// </summary>
        [JsonProperty(PropertyName = "discoveryProfileUri",
            NullValueHandling = NullValueHandling.Ignore)]
        public string DiscoveryProfileUri { get; set; }
    }
}
