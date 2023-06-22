// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    /// <summary>
    /// Discovery configuration model extensions
    /// </summary>
    public static class DiscoveryConfigModelEx
    {
        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static DiscoveryConfigModel? Clone(this DiscoveryConfigModel? model)
        {
            return model == null ? null : (model with
            {
                DiscoveryUrls = model.DiscoveryUrls?.Count > 0 ?
                    model.DiscoveryUrls.ToList() : null,
                Locales = model.Locales?.Count > 0 ?
                    model.Locales.ToList() : null,
                PortRangesToScan = string.IsNullOrEmpty(model.PortRangesToScan) ?
                    null : model.PortRangesToScan,
                AddressRangesToScan = string.IsNullOrEmpty(model.AddressRangesToScan) ?
                    null : model.AddressRangesToScan,
                IdleTimeBetweenScans = model.IdleTimeBetweenScans < TimeSpan.Zero ?
                    null : model.IdleTimeBetweenScans,
                MaxNetworkProbes = model.MaxNetworkProbes <= 0 ?
                    null : model.MaxNetworkProbes,
                MaxPortProbes = model.MaxPortProbes <= 0 ?
                    null : model.MaxPortProbes,
                MinPortProbesPercent = model.MinPortProbesPercent <= 0 ?
                    null : model.MinPortProbesPercent,
                NetworkProbeTimeout = model.NetworkProbeTimeout <= TimeSpan.Zero ?
                    null : model.NetworkProbeTimeout,
                PortProbeTimeout = model.PortProbeTimeout <= TimeSpan.Zero ?
                    null : model.PortProbeTimeout
            });
        }
    }
}
