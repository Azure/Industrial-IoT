// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Models {

    /// <summary>
    /// Request node browsing service
    /// </summary>
    public class BrowseRequestModel {

        /// <summary>
        /// Node to browse, or null for root.
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// Direction to browse in
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
        /// If not set, implies false
        /// </summary>
        public uint? MaxReferencesToReturn { get; set; }
    }
}
