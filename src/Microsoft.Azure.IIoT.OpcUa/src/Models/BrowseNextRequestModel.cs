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
        /// Continuation token to use
        /// </summary>
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Whether to abort browse and release
        /// </summary>
        public bool? Abort { get; set; }

        /// <summary>
        /// Elevation
        /// </summary>
        public AuthenticationModel Elevation { get; set; }
    }
}
