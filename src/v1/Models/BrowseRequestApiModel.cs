// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;

    /// <summary>
    /// Browse request model for twin module
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
            TargetNodesOnly = model.TargetNodesOnly;
            ReadVariableValues = model.ReadVariableValues;
            NoSubtypes = model.NoSubtypes;
            Elevation = model.Elevation == null ? null :
                new CredentialApiModel(model.Elevation);
            Diagnostics = model.Diagnostics == null ? null :
                new DiagnosticsApiModel(model.Diagnostics);
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
                TargetNodesOnly = TargetNodesOnly,
                ReadVariableValues = ReadVariableValues,
                NoSubtypes = NoSubtypes,
                Diagnostics = Diagnostics?.ToServiceModel(),
                Elevation = Elevation?.ToServiceModel()
            };
        }

        /// <summary>
        /// Node to browse.
        /// (default: RootFolder).
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// Direction to browse in
        /// (default: forward)
        /// </summary>
        public BrowseDirection? Direction { get; set; }

        /// <summary>
        /// View to browse
        /// (default: null = new view = All nodes).
        /// </summary>
        public BrowseViewApiModel View { get; set; }

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
        /// Whether to collapse all references into a set of
        /// unique target nodes and not show reference
        /// information.
        /// (default is false)
        /// </summary>
        public bool? TargetNodesOnly { get; set; }

        /// <summary>
        /// Whether to read variable values on target nodes.
        /// (default is false)
        /// </summary>
        public bool? ReadVariableValues { get; set; }

        /// <summary>
        /// Max number of references to return. There might
        /// be less returned as this is up to the client
        /// restrictions.  Set to 0 to return no references
        /// or target nodes.
        /// (default is decided by client e.g. 60)
        /// </summary>
        public uint? MaxReferencesToReturn { get; set; }

        /// <summary>
        /// Optional elevation.
        /// </summary>
        public CredentialApiModel Elevation { get; set; }

        /// <summary>
        /// Optional diagnostics configuration
        /// </summary>
        public DiagnosticsApiModel Diagnostics { get; set; }
    }
}
