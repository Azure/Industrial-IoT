// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {

    /// <summary>
    /// Handle event
    /// </summary>
    public static class DiscovererApiModelEx {

        /// <summary>
        /// Update a discover
        /// </summary>
        /// <param name="discoverer"></param>
        /// <param name="update"></param>
        /// <param name="isPatch"></param>
        public static DiscovererApiModel Patch(this DiscovererApiModel update,
            DiscovererApiModel discoverer, bool isPatch = false) {
            if (discoverer == null) {
                return update;
            }
            if (!isPatch || update.Connected != null) {
                discoverer.Connected = update.Connected;
            }
            if (!isPatch || update.Discovery != null) {
                discoverer.Discovery = update.Discovery;
            }
            if (!isPatch || update.Id != null) {
                discoverer.Id = update.Id;
            }
            if (!isPatch || update.LogLevel != null) {
                discoverer.LogLevel = update.LogLevel;
            }
            if (!isPatch || update.OutOfSync != null) {
                discoverer.OutOfSync = update.OutOfSync;
            }
            if (!isPatch || update.SiteId != null) {
                discoverer.SiteId = update.SiteId;
            }
            discoverer.DiscoveryConfig = update.DiscoveryConfig.Patch(
                discoverer.DiscoveryConfig, isPatch);
            return discoverer;
        }
    }
}
