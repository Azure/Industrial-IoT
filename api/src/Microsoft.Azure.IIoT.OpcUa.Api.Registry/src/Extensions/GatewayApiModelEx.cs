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
        /// <param name="isPatch"></param>
        public static GatewayApiModel Patch(this GatewayApiModel update,
            GatewayApiModel gateway, bool isPatch = false) {
            if (gateway == null) {
                return update;
            }
            if (!isPatch || update.Connected != null) {
                gateway.Connected = update.Connected;
            }
            if (!isPatch || update.Id != null) {
                gateway.Id = update.Id;
            }
            if (!isPatch || update.SiteId != null) {
                gateway.SiteId = update.SiteId;
            }
            return gateway;
        }
    }
}
