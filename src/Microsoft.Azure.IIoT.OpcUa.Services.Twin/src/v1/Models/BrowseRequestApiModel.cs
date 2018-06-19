// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Models;
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
            MaxReferencesToReturn = model.MaxReferencesToReturn;
            Direction = model.Direction;
            ReferenceTypeId = model.ReferenceTypeId;
            NoSubtypes = model.NoSubtypes;
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
                ReferenceTypeId = ReferenceTypeId,
                NoSubtypes = NoSubtypes
            };
        }

        /// <summary>
        /// Node to browse, or null for root.
        /// </summary>
        [JsonProperty(PropertyName = "nodeId")]
        [Required]
        public string NodeId { get; set; }

        /// <summary>
        /// Direction to browse in
        /// </summary>
        [JsonProperty(PropertyName = "direction",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public BrowseDirection? Direction { get; set; }

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
        /// If not set, implies reasonable default
        /// </summary>
        [JsonProperty(PropertyName = "maxReferencesToReturn",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public uint? MaxReferencesToReturn { get; set; }
    }
}
