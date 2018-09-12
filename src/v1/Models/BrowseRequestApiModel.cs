// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Newtonsoft.Json;
    using System.ComponentModel;

    /// <summary>
    /// Browse request model for webservice api
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
            MaxReferencesToReturn = model.MaxReferencesToReturn;
            Direction = model.Direction;
            ReferenceTypeId = model.ReferenceTypeId;
            NoSubtypes = model.NoSubtypes;
            TargetNodesOnly = model.TargetNodesOnly;
            Elevation = model.Elevation == null ? null :
                new AuthenticationApiModel(model.Elevation);
            View = model.View == null ? null :
                new BrowseViewApiModel(model.View);
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public BrowseRequestModel ToServiceModel() {
            return new BrowseRequestModel {
                NodeId = NodeId,
                MaxReferencesToReturn = MaxReferencesToReturn,
                Direction = Direction,
                View = View?.ToServiceModel(),
                ReferenceTypeId = ReferenceTypeId,
                Elevation = Elevation?.ToServiceModel(),
                TargetNodesOnly = TargetNodesOnly,
                NoSubtypes = NoSubtypes
            };
        }

        /// <summary>
        /// Node to browse.
        /// (default: ObjectRoot).
        /// </summary>
        [JsonProperty(PropertyName = "nodeId")]
        public string NodeId { get; set; }

        /// <summary>
        /// Direction to browse in
        /// (default: forward)
        /// </summary>
        [JsonProperty(PropertyName = "direction",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public BrowseDirection? Direction { get; set; }

        /// <summary>
        /// View to browse
        /// (default: null = new view = All nodes).
        /// </summary>
        [JsonProperty(PropertyName = "view",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public BrowseViewApiModel View { get; set; }

        /// <summary>
        /// Reference types to browse.
        /// (default: hierarchical).
        /// </summary>
        [JsonProperty(PropertyName = "referenceTypeId",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string ReferenceTypeId { get; set; }

        /// <summary>
        /// Whether to include subtypes of the reference type.
        /// (default is false)
        /// </summary>
        [JsonProperty(PropertyName = "noSubtypes",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(false)]
        public bool? NoSubtypes { get; set; }

        /// <summary>
        /// Max number of references to return. There might
        /// be less returned as this is up to the client
        /// restrictions.  Set to 0 to return no references
        /// or target nodes.
        /// (default is decided by client e.g. 60)
        /// </summary>
        [JsonProperty(PropertyName = "maxReferencesToReturn",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public uint? MaxReferencesToReturn { get; set; }

        /// <summary>
        /// Whether to collapse all references into a set of
        /// unique target nodes and not show reference
        /// information.
        /// (default is false)
        /// </summary>
        [JsonProperty(PropertyName = "targetNodesOnly",
           NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(false)]
        public bool? TargetNodesOnly { get; set; }

        /// <summary>
        /// Optional User elevation
        /// </summary>
        [JsonProperty(PropertyName = "elevation",
            NullValueHandling = NullValueHandling.Ignore)]
        public AuthenticationApiModel Elevation { get; set; }
    }
}
