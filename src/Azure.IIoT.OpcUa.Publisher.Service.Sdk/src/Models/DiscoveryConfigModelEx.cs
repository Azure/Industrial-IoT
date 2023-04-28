// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk
{
    using Azure.IIoT.OpcUa.Publisher.Models;

    /// <summary>
    /// Discovery config api model extensions
    /// </summary>
    public static class DiscoveryConfigModelEx
    {
        /// <summary>
        /// Update an config
        /// </summary>
        /// <param name="update"></param>
        /// <param name="config"></param>
        public static DiscoveryConfigModel? Patch(this DiscoveryConfigModel? update,
            DiscoveryConfigModel? config)
        {
            if (update == null)
            {
                return config;
            }
            config ??= new DiscoveryConfigModel();
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
