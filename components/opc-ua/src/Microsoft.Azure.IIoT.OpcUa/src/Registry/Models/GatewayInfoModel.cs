// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {

    /// <summary>
    /// Edge Gateway info
    /// </summary>
    public class GatewayInfoModel {

        /// <summary>
        /// Identifier of the gateway
        /// </summary>
        public GatewayModel Gateway { get; set; }

        /// <summary>
        /// Gateway modules
        /// </summary>
        public GatewayModulesModel Modules { get; set; }
    }
}
