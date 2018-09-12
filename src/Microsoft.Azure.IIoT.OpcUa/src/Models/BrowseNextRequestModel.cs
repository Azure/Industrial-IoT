// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Models {

    /// <summary>
    /// Request node browsing continuation
    /// </summary>
    public class BrowseNextRequestModel {

        /// <summary>
        /// Continuation token from previews browse request.
        /// (mandatory)
        /// </summary>
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Whether to abort browse and release.
        /// (default: false)
        /// </summary>
        public bool? Abort { get; set; }

        /// <summary>
        /// Whether to collapse all references into a set of
        /// unique target nodes and not show reference
        /// information.
        /// (default is false)
        /// </summary>
        public bool? TargetNodesOnly { get; set; }

        /// <summary>
        /// Optional elevation
        /// </summary>
        public AuthenticationModel Elevation { get; set; }
    }
}
