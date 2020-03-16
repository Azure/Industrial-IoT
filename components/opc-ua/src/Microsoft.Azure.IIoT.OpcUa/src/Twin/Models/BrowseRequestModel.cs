// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Collections.Generic;

    /// <summary>
    /// Request node browsing service
    /// </summary>
    public class BrowseRequestModel {

        /// <summary>
        /// Node to browse.
        /// (defaults to root folder).
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
        public BrowseViewModel View { get; set; }

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
        /// Max number of references to return. There might
        /// be less returned as this is up to the client
        /// restrictions.  Set to 0 to return no references
        /// or target nodes.
        /// (default is decided by client e.g. 60)
        /// </summary>
        public uint? MaxReferencesToReturn { get; set; }

        /// <summary>
        /// Whether to collapse all references into a set of
        /// unique target nodes and not show reference
        /// information.
        /// (default is false)
        /// </summary>
        public bool? TargetNodesOnly { get; set; }

        /// <summary>
        /// Whether to read variable values of target nodes.
        /// (default is false)
        /// </summary>
        public bool? ReadVariableValues { get; set; }

        /// <summary>
        /// Whether to only return the raw node id
        /// information and not read the target node.
        /// (default is false)
        /// </summary>
        public bool? NodeIdsOnly { get; set; }

        /// <summary>
        /// Filter returned target nodes by only returning
        /// nodes that have classes in this array.
        /// (default: null - all targets are returned)
        /// </summary>
        public List<NodeClass> NodeClassFilter { get; set; }

        /// <summary>
        /// Optional header
        /// </summary>
        public RequestHeaderModel Header { get; set; }
    }
}
