// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Published nodes request
    /// </summary>
    public class PublishNodesRequestModel {

        /// <summary>
        /// Endpoint identifier on the publisher (optional)
        /// </summary>
        [JsonProperty(PropertyName = "EndpointId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string EndpointId { get; set; }

        /// <summary>
        /// Endpoint url (mandatory)
        /// </summary>
        [JsonProperty(PropertyName = "EndpointUrl",
            NullValueHandling = NullValueHandling.Include)]
        public string EndpointUrl { get; set; }

        /// <summary>
        /// Whether to use secure connectivity (default: true)
        /// </summary>
        [JsonProperty(PropertyName = "UseSecurity",
           NullValueHandling = NullValueHandling.Include)]
        public bool? UseSecurity { get; set; }

        /// <summary>
        /// Endpoint security profile uri (optional)
        /// </summary>
        [JsonProperty(PropertyName = "SecurityProfileUri",
            NullValueHandling = NullValueHandling.Ignore)]
        public string SecurityProfileUri { get; set; }

        /// <summary>
        /// Endpoint security mode name (optional)
        /// </summary>
        [JsonProperty(PropertyName = "SecurityMode",
            NullValueHandling = NullValueHandling.Ignore)]
        public string SecurityMode { get; set; }

        /// <summary>
        /// User name for user name password credential
        /// </summary>
        [JsonProperty(PropertyName = "UserName",
           NullValueHandling = NullValueHandling.Include)]
        public string UserName { get; set; }

        /// <summary>
        /// Password for user name password credential
        /// </summary>
        [JsonProperty(PropertyName = "Password",
           NullValueHandling = NullValueHandling.Include)]
        public string Password { get; set; }

        /// <summary>
        /// Nodes to publish
        /// </summary>
        [JsonProperty(PropertyName = "OpcNodes",
           NullValueHandling = NullValueHandling.Include)]
        public List<PublisherNodeModel> OpcNodes { get; set; }
    }
}
