// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Edge.Module.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Services.Models;

    /// <summary>
    /// browse request model for edge service api
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
        public string NodeId { get; set; }

        /// <summary>
        /// Direction to browse
        /// </summary>
        public BrowseDirection? Direction { get; set; }

        /// <summary>
        /// Reference types to browse.
        /// (default: hierarchical).
        /// </summary>
        public string ReferenceTypeId { get; set; }

        /// <summary>
        /// Whether to include subtypes of the reference type.
        /// (default is false)
        /// </summary>
        public bool? NoSubtypes { get; set; }

        /// <summary>
        /// If not set, implies stack default, 0 to exclude
        /// </summary>
        public uint? MaxReferencesToReturn { get; set; }
    }
}
