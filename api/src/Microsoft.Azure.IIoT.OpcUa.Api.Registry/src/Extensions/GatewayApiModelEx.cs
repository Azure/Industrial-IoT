// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {

    /// <summary>
    /// Handle event
    /// </summary>
    public static class GatewayApiModelEx {

        /// <summary>
        /// Update a discover
        /// </summary>
        /// <param name="gateway"></param>
        /// <param name="update"></param>
        public static GatewayApiModel Patch(this GatewayApiModel update,
            GatewayApiModel gateway) {
            if (update == null) {
                return gateway;
            }
            if (gateway == null) {
                return new GatewayApiModel();
            }
            gateway.Connected = update.Connected;
            gateway.Id = update.Id;
            gateway.SiteId = update.SiteId;
            return gateway;
        }
    }
}
