// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {

    /// <summary>
    /// Discovery config api model extensions
    /// </summary>
    public static class DiscoveryConfigApiModelEx {

        /// <summary>
        /// Update an config
        /// </summary>
        /// <param name="config"></param>
        /// <param name="update"></param>
        public static DiscoveryConfigApiModel Patch(this DiscoveryConfigApiModel update,
            DiscoveryConfigApiModel config) {
            if (update == null) {
                return config;
            }
            if (config == null) {
                config = new DiscoveryConfigApiModel();
            }
            config.ActivationFilter = update.ActivationFilter;
            config.AddressRangesToScan = update.AddressRangesToScan;
            config.DiscoveryUrls = update.DiscoveryUrls;
            config.IdleTimeBetweenScans = update.IdleTimeBetweenScans;
            config.Locales = update.Locales;
            config.MaxNetworkProbes = update.MaxNetworkProbes;
            config.MaxPortProbes = update.MaxPortProbes;
            config.MinPortProbesPercent = update.MinPortProbesPercent;
            config.NetworkProbeTimeout = update.NetworkProbeTimeout;
            config.PortProbeTimeout = update.PortProbeTimeout;
            config.PortRangesToScan = update.PortRangesToScan;
            return config;
        }
    }
}
