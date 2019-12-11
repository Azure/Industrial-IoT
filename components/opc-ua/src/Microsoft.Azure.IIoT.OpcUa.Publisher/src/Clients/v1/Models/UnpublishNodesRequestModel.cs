// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Unpublish nodes request
    /// </summary>
    public class UnpublishNodesRequestModel {

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
        /// Nodes to publish
        /// </summary>
        [JsonProperty(PropertyName = "OpcNodes",
           NullValueHandling = NullValueHandling.Include)]
        public List<PublisherNodeModel> OpcNodes { get; set; }
    }
}
