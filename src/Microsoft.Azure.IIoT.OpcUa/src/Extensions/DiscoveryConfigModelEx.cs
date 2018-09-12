// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Models {
    using System.Linq;

    /// <summary>
    /// Discovery configuration model extensions
    /// </summary>
    public static class DiscoveryConfigModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DiscoveryConfigModel Clone(this DiscoveryConfigModel model) {
            if (model == null) {
                return null;
            }
            return new DiscoveryConfigModel {
                ActivationFilter = model.ActivationFilter.Clone(),
                AddressRangesToScan = model.AddressRangesToScan,
                Callbacks = model.Callbacks?.Select(c => c.Clone()).ToList(),
                DiscoveryUrls = model.DiscoveryUrls?.ToList(),
                IdleTimeBetweenScans = model.IdleTimeBetweenScans,
                MaxNetworkProbes = model.MaxNetworkProbes,
                MaxPortProbes = model.MaxPortProbes,
                MinPortProbesPercent = model.MinPortProbesPercent,
                NetworkProbeTimeout = model.NetworkProbeTimeout,
                PortProbeTimeout = model.PortProbeTimeout,
                PortRangesToScan = model.PortRangesToScan
            };
        }
    }
}
