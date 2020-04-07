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
        public static ApplicationInfoApiModel Patch(this ApplicationInfoApiModel update,
            ApplicationInfoApiModel application) {
            if (update == null) {
                return application;
            }
            if (application == null) {
                application = new ApplicationInfoApiModel();
            }
            application.ApplicationId = update.ApplicationId;
            application.ApplicationName = update.ApplicationName;
            application.ApplicationType = update.ApplicationType;
            application.ApplicationUri = update.ApplicationUri;
            application.Capabilities = update.Capabilities;
            application.Created = update.Created;
            application.DiscovererId = update.DiscovererId;
            application.DiscoveryProfileUri = update.DiscoveryProfileUri;
            application.DiscoveryUrls = update.DiscoveryUrls;
            application.GatewayServerUri = update.GatewayServerUri;
            application.HostAddresses = update.HostAddresses;
            application.Locale = update.Locale;
            application.LocalizedNames = update.LocalizedNames;
            application.NotSeenSince = update.NotSeenSince;
            application.ProductUri = update.ProductUri;
            application.SiteId = update.SiteId;
            application.Updated = update.Updated;
            return application;
        }
    }
}
