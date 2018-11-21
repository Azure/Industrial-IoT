// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Clients.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Class describing a node
    /// </summary>
    public class PublisherNodeOnEndpointModel {

        /// <summary>
        /// Id can be:
        /// a NodeId ("ns=")
        /// an ExpandedNodeId ("nsu=")
        /// </summary>
        [JsonProperty(PropertyName = "Id",
            NullValueHandling = NullValueHandling.Include)]
        public string Id;

        /// <summary>
        /// Support legacy configuration file syntax
        /// </summary>
        [JsonProperty(PropertyName = "ExpandedNodeId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ExpandedNodeId;

        /// <summary>
        /// Whether to use security
        /// </summary>
        [JsonProperty(PropertyName = "OpcSamplingInterval",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? OpcSamplingInterval;

        /// <summary>
        /// Whether to use security
        /// </summary>
        [JsonProperty(PropertyName = "OpcPublishingInterval",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? OpcPublishingInterval;
    }
}
