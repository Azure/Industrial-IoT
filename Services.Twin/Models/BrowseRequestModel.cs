// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models {

    /// <summary>
    /// Request node browsing service
    /// </summary>
    public class BrowseRequestModel {

        /// <summary>
        /// Node to browse, or null for root.
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// If not set, implies false
        /// </summary>
        public bool? ExcludeReferences { get; set; }

        /// <summary>
        /// If not set, implies false
        /// </summary>
        public bool? IncludePublishingStatus { get; set; }

        /// <summary>
        /// Optional parent node to include in node result
        /// </summary>
        public string Parent { get; set; }
    }
}
