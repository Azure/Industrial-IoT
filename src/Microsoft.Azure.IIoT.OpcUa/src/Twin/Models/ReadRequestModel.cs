// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Collections.Generic;

    /// <summary>
    /// Request node attribute read or update
    /// </summary>
    public class ReadRequestModel {

        /// <summary>
        /// Attributes to update or read
        /// </summary>
        public List<AttributeReadRequestModel> Attributes { get; set; }

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
