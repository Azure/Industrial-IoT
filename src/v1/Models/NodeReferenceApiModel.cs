// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// reference model for webservice api
    /// </summary>
    public class NodeReferenceApiModel {
        /// <summary>
        /// Default constructor
        /// </summary>
        public NodeReferenceApiModel() {}

        /// <summary>
        /// Create reference api model
        /// </summary>
        /// <param name="model"></param>
        public NodeReferenceApiModel(NodeReferenceModel model) {
            Id = model.Id;
            BrowseName = model.BrowseName;
            DisplayName = model.DisplayName;
            Direction = model.Direction;
            Target = new NodeApiModel(model.Target);
        }

        /// <summary>
        /// Reference Type id
        /// </summary>
        [JsonProperty(PropertyName = "id",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        /// <summary>
        /// Browse name of reference
        /// </summary>
        [JsonProperty(PropertyName = "browseName",
            NullValueHandling = NullValueHandling.Ignore)]
        public string BrowseName { get; set; }

        /// <summary>
        /// Browse direction of reference
        /// </summary>
        [JsonProperty(PropertyName = "direction",
            NullValueHandling = NullValueHandling.Ignore)]
        public BrowseDirection? Direction { get; set; }

        /// <summary>
        /// Display name of reference
        /// </summary>
        [JsonProperty(PropertyName = "displayName",
            NullValueHandling = NullValueHandling.Ignore)]
        public string DisplayName { get; set; }

        /// <summary>
        /// Target node
        /// </summary>
        [JsonProperty(PropertyName = "target")]
        [Required]
        public NodeApiModel Target { get; set; }
    }
}
