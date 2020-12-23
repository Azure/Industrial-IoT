// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {

    /// <summary>
    /// Edge Gateway registration query request
    /// </summary>
    public class GatewayQueryModel {

        /// <summary>
        /// Site of the gateways
        /// </summary>
        public string SiteId { get; set; }

        /// <summary>
        /// Included connected or disconnected
        /// </summary>
        public bool? Connected { get; set; }
    }
}
