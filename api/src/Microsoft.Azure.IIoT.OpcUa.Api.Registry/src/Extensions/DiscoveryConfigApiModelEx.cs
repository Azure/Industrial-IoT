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
        /// <param name="isPatch"></param>
        public static DiscoveryConfigApiModel Patch(this DiscoveryConfigApiModel update,
            DiscoveryConfigApiModel config, bool isPatch = false) {
            if (config == null) {
                return update;
            }
            if (!isPatch || update.ActivationFilter != null) {
                config.ActivationFilter = update.ActivationFilter;
            }
            if (!isPatch || update.AddressRangesToScan != null) {
                config.AddressRangesToScan = update.AddressRangesToScan;
            }
            if (!isPatch || update.DiscoveryUrls != null) {
                config.DiscoveryUrls = update.DiscoveryUrls;
            }
            if (!isPatch || update.IdleTimeBetweenScansSec != null) {
                config.IdleTimeBetweenScansSec = update.IdleTimeBetweenScansSec;
            }
            if (!isPatch || update.Locales != null) {
                config.Locales = update.Locales;
            }
            if (!isPatch || update.MaxNetworkProbes != null) {
                config.MaxNetworkProbes = update.MaxNetworkProbes;
            }
            if (!isPatch || update.MaxPortProbes != null) {
                config.MaxPortProbes = update.MaxPortProbes;
            }
            if (!isPatch || update.MinPortProbesPercent != null) {
                config.MinPortProbesPercent = update.MinPortProbesPercent;
            }
            if (!isPatch || update.NetworkProbeTimeoutMs != null) {
                config.NetworkProbeTimeoutMs = update.NetworkProbeTimeoutMs;
            }
            if (!isPatch || update.PortProbeTimeoutMs != null) {
                config.PortProbeTimeoutMs = update.PortProbeTimeoutMs;
            }
            if (!isPatch || update.PortRangesToScan != null) {
                config.PortRangesToScan = update.PortRangesToScan;
            }
            return config;
        }
    }
}
