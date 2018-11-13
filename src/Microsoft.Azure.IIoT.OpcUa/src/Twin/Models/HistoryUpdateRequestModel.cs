// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Request node history update
    /// </summary>
    public class HistoryUpdateRequestModel {

        /// <summary>
        /// The HistoryUpdateDetailsType extension object
        /// encoded in json and containing the tunneled
        /// update request for the Historian server.
        /// </summary>
        public JToken Request { get; set; }

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
