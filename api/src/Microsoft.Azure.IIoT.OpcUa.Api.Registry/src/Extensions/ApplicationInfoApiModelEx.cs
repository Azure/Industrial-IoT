// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {

    /// <summary>
    /// Handle event
    /// </summary>
    public static class ApplicationInfoApiModelEx {

        /// <summary>
        /// Update an application
        /// </summary>
        /// <param name="application"></param>
        /// <param name="update"></param>
        /// <param name="isPatch"></param>
        public static ApplicationInfoApiModel Patch(this ApplicationInfoApiModel update,
            ApplicationInfoApiModel application, bool isPatch = false) {
            if (application == null) {
                return update;
            }
            if (!isPatch || update.ApplicationId != null) {
                application.ApplicationId = update.ApplicationId;
            }
            if (!isPatch || update.ApplicationName != null) {
                application.ApplicationName = update.ApplicationName;
            }
            if (!isPatch) {
                application.ApplicationType = update.ApplicationType;
            }
            if (!isPatch || update.ApplicationUri != null) {
                application.ApplicationUri = update.ApplicationUri;
            }
            if (!isPatch || update.Capabilities != null) {
                application.Capabilities = update.Capabilities;
            }
            if (!isPatch || update.Created != null) {
                application.Created = update.Created;
            }
            if (!isPatch || update.DiscovererId != null) {
                application.DiscovererId = update.DiscovererId;
            }
            if (!isPatch || update.DiscoveryProfileUri != null) {
                application.DiscoveryProfileUri = update.DiscoveryProfileUri;
            }
            if (!isPatch || update.DiscoveryUrls != null) {
                application.DiscoveryUrls = update.DiscoveryUrls;
            }
            if (!isPatch || update.GatewayServerUri != null) {
                application.GatewayServerUri = update.GatewayServerUri;
            }
            if (!isPatch || update.HostAddresses != null) {
                application.HostAddresses = update.HostAddresses;
            }
            if (!isPatch || update.Locale != null) {
                application.Locale = update.Locale;
            }
            if (!isPatch || update.LocalizedNames != null) {
                application.LocalizedNames = update.LocalizedNames;
            }
            if (!isPatch || update.NotSeenSince != null) {
                application.NotSeenSince = update.NotSeenSince;
            }
            if (!isPatch || update.ProductUri != null) {
                application.ProductUri = update.ProductUri;
            }
            if (!isPatch || update.SiteId != null) {
                application.SiteId = update.SiteId;
            }
            if (!isPatch || update.Updated != null) {
                application.Updated = update.Updated;
            }
            return application;
        }
    }
}
