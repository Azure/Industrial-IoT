// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Collections.Generic;

    /// <summary>
    /// Node method call service request
    /// </summary>
    public class MethodCallRequestModel {

        /// <summary>
        /// Object scope of the method
        /// </summary>
        public string ObjectId { get; set; }

        /// <summary>
        /// Method to call
        /// </summary>
        public string MethodId { get; set; }

        /// <summary>
        /// Input Arguments
        /// </summary>
        public List<MethodCallArgumentModel> Arguments { get; set; }

        /// <summary>
        /// Elevation
        /// </summary>
        public CredentialModel Elevation { get; set; }

        /// <summary>
        /// Optional diagnostics configuration
        /// </summary>
        public DiagnosticsModel Diagnostics { get; set; }
    }
}
