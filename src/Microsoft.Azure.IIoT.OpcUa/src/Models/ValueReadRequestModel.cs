// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Models {

    /// <summary>
    /// Request node value read
    /// </summary>
    public class ValueReadRequestModel {

        /// <summary>
        /// Node to read from
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// Elevation
        /// </summary>
        public AuthenticationModel Elevation { get; set; }
    }
}
