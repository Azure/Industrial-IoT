// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;

    /// <summary>
    /// Request node history read continuation
    /// </summary>
    public class HistoryReadNextRequestModel {

        /// <summary>
        /// Continuation token to continue reading more
        /// results.
        /// </summary>
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Abort reading after this read
        /// </summary>
        public bool? Abort { get; set; }

        /// <summary>
        /// Optional User Elevation
        /// </summary>
        public CredentialModel Elevation { get; set; }

        /// <summary>
        /// Optional diagnostics configuration
        /// </summary>
        public DiagnosticsModel Diagnostics { get; set; }
    }
}
