// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Edge.Discovery {
    using Microsoft.Azure.IIoT.Net.Models;
    using Microsoft.Azure.IIoT.OpcUa.Services.Models;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Server discovery run configuration
    /// </summary>
    public class OpcUaDiscoveryOptions {

        public static OpcUaDiscoveryOptions Default => new OpcUaDiscoveryOptions();

        /// <summary>
        /// Discovery mode
        /// </summary>
        public DiscoveryMode Mode { get; set; } = DiscoveryMode.Off;

        /// <summary>
        /// Discovery configuration
        /// </summary>
        public DiscoveryConfigModel Configuration { get; set; } = new DiscoveryConfigModel();

        /// <summary>
        /// Network class
        /// </summary>
        public NetworkClass NetworkClass { get; set; } = NetworkClass.Wired;

        /// <summary>
        /// Address ranges to use or null to use from network info
        /// </summary>
        public IEnumerable<AddressRange> AddressRanges { get; set; }

        /// <summary>
        /// Port ranges to use if not from discovery mode
        /// </summary>
        public IEnumerable<PortRange> PortRanges { get; set; }

        /// <summary>
        /// Clone options
        /// </summary>
        /// <returns></returns>
        internal OpcUaDiscoveryOptions Clone() {
            var options = new OpcUaDiscoveryOptions {
                NetworkClass = NetworkClass
            };
            options.UpdateFromModel(Mode, Configuration);
            return options;
        }

        /// <summary>
        /// Update ranges on option
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        internal bool UpdateFromModel(DiscoveryMode mode,
            DiscoveryConfigModel configuration) {

            Configuration.IdleTimeBetweenScans = configuration?.IdleTimeBetweenScans;

            var restart =
                Mode != mode ||
                Configuration.PortProbeTimeout != configuration?.PortProbeTimeout ||
                Configuration.NetworkProbeTimeout != configuration?.NetworkProbeTimeout ||
                Configuration.MaxPortProbes != configuration?.MaxPortProbes ||
                Configuration.MaxNetworkProbes != configuration?.MaxNetworkProbes;

            if (!string.IsNullOrEmpty(configuration?.AddressRangesToScan)) {
                if (AddressRange.TryParse(configuration.AddressRangesToScan,
                    out var addresses)) {
                    AddressRanges = addresses;
                    restart = true;
                }
            }
            else {
                if (AddressRanges != null) {
                    AddressRanges = null;
                    restart = true;
                }
            }

            if (!string.IsNullOrEmpty(configuration?.PortRangesToScan)) {
                if (PortRange.TryParse(configuration.PortRangesToScan,
                    out var ports)) {
                    PortRanges = ports;
                    restart = true;
                }
            }
            else {
                if (PortRanges != null) {
                    PortRanges = null;
                    restart = true;
                }
            }

            Mode = mode;

            Configuration.AddressRangesToScan = configuration?.AddressRangesToScan;
            Configuration.PortRangesToScan = configuration?.PortRangesToScan;
            Configuration.PortProbeTimeout = configuration?.PortProbeTimeout;
            Configuration.NetworkProbeTimeout = configuration?.NetworkProbeTimeout;
            Configuration.MaxPortProbes = configuration?.MaxPortProbes;
            Configuration.MinPortProbesPercent = configuration?.MinPortProbesPercent;
            Configuration.MaxNetworkProbes = configuration?.MaxNetworkProbes;

            return restart;
        }
    }
}
