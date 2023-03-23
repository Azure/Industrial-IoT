// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk
{
    using Azure.IIoT.OpcUa.Publisher.Models;

    /// <summary>
    /// Handle event
    /// </summary>
    public static class GatewayModelEx
    {
        /// <summary>
        /// Update a discover
        /// </summary>
        /// <param name="update"></param>
        /// <param name="gateway"></param>
        public static GatewayModel Patch(this GatewayModel update,
            GatewayModel gateway)
        {
            if (update == null)
            {
                return gateway;
            }
            if (gateway == null)
            {
                return new GatewayModel();
            }
            gateway.Connected = update.Connected;
            gateway.Id = update.Id;
            gateway.SiteId = update.SiteId;
            return gateway;
        }
    }
}
