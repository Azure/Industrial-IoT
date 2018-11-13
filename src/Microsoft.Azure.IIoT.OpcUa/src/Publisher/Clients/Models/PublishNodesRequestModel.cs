// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Clients.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Published nodes request
    /// </summary>
    public class PublishNodesRequestModel {

        /// <summary>
        /// Endpoint url
        /// </summary>
        [JsonProperty(PropertyName = "EndpointUrl",
            NullValueHandling = NullValueHandling.Include)]
        public string EndpointUrl { get; set; }

        /// <summary>
        /// Nodes to publish
        /// </summary>
        [JsonProperty(PropertyName = "Nodes",
           NullValueHandling = NullValueHandling.Include)]
        public List<PublisherNodeModel> Nodes { get; set; }

        /// <summary>
        /// Whether to use secure connectivity
        /// </summary>
        [JsonProperty(PropertyName = "UseSecurity",
           NullValueHandling = NullValueHandling.Include)]
        public bool UseSecurity { get; set; }

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
    }
}
