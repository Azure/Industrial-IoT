// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.WebService.v1.Models {
    using Microsoft.Azure.IIoT.OpcTwin.Services.Models;
    using Newtonsoft.Json;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// browse request model for webservice api
    /// </summary>
    public class BrowseRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public BrowseRequestApiModel() {}

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public BrowseRequestApiModel(BrowseRequestModel model) {
            NodeId = model.NodeId;
            ExcludeReferences = model.ExcludeReferences;
            Parent = model.Parent;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public BrowseRequestModel ToServiceModel() {
            return new BrowseRequestModel {
                NodeId = NodeId,
                ExcludeReferences = ExcludeReferences,
                Parent = Parent
            };
        }

        /// <summary>
        /// Node to browse, or null for root.
        /// </summary>
        [JsonProperty(PropertyName = "nodeId")]
        [Required]
        public string NodeId { get; set; }

        /// <summary>
        /// If not set, implies false
        /// </summary>
        [JsonProperty(PropertyName = "excludeReferences",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(false)]
        public bool? ExcludeReferences { get; set; }

        /// <summary>
        /// Optional parent node to include in node result
        /// </summary>
        [JsonProperty(PropertyName = "parentId",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string Parent { get; set; }
    }
}
