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
    public static class DiscovererModelEx2
    {
        /// <summary>
        /// Update a discover
        /// </summary>
        /// <param name="update"></param>
        /// <param name="discoverer"></param>
        public static DiscovererModel Patch(this DiscovererModel update,
            DiscovererModel discoverer)
        {
            if (update == null)
            {
                return discoverer;
            }
            discoverer ??= new DiscovererModel();
            discoverer.Connected = update.Connected;
            discoverer.Discovery = update.Discovery;
            discoverer.Id = update.Id;
            discoverer.OutOfSync = update.OutOfSync;
            discoverer.SiteId = update.SiteId;
            discoverer.RequestedMode = update.RequestedMode;
            discoverer.RequestedConfig = (update.RequestedConfig ?? new DiscoveryConfigModel())
                .Patch(discoverer.RequestedConfig);
            discoverer.DiscoveryConfig = (update.DiscoveryConfig ?? new DiscoveryConfigModel())
                .Patch(discoverer.DiscoveryConfig);
            return discoverer;
        }
    }
}
