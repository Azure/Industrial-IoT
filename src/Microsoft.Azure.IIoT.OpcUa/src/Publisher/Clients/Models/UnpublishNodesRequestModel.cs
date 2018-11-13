// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Clients.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Unpublished nodes request
    /// </summary>
    public class UnpublishNodesRequestModel {

        /// <summary>
        /// Endpoint url
        /// </summary>
        [JsonProperty(PropertyName = "EndpointUrl",
            NullValueHandling = NullValueHandling.Include)]
        public string EndpointUrl { get; set; }

        /// <summary>
        /// Nodes to unpublish - only node id is used from it.
        /// </summary>
        [JsonProperty(PropertyName = "Nodes",
            NullValueHandling = NullValueHandling.Include)]
        public List<PublisherNodeModel> Nodes { get; set; }
    }
}
