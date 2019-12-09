// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Request header model
    /// </summary>
    public class RequestHeaderModel {

        /// <summary>
        /// Optional User Elevation
        /// </summary>
        public CredentialModel Elevation { get; set; }

        /// <summary>
        /// Optional list of locales in preference order.
        /// </summary>
        public List<string> Locales { get; set; }

        /// <summary>
        /// Optional diagnostics configuration
        /// </summary>
        public DiagnosticsModel Diagnostics { get; set; }
    }
}
