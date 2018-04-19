// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService.Discovery {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
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
        /// Max parallel threads to execute discovery process
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

        /// <summary>
        /// Delay time between discovery sweeps
        /// </summary>
        public TimeSpan? DiscoveryIdleTime { get; set; }

        /// <summary>
        /// Address ranges to use or null to use from network info
        /// </summary>
        public IEnumerable<AddressRange> AddressRanges { get; set; }

        /// <summary>
        /// Network probe timeout
        /// </summary>
        public TimeSpan? NetworkProbeTimeout { get; set; }

        /// <summary>
        /// Max network probes that should ever run.
        /// </summary>
        public int? MaxNetworkProbes { get; set; }

        /// <summary>
        /// Network class
        /// </summary>
        public NetworkClass NetworkClass { get; set; } = NetworkClass.Wired;

        /// <summary>
        /// Port ranges to use if not from discovery mode
        /// </summary>
        public IEnumerable<PortRange> PortRanges { get; set; }

        /// <summary>
        /// Minimum port probes that should run.
        /// </summary>
        public TimeSpan? PortProbeTimeout { get; set; }

        /// <summary>
        /// Max port probes that should ever run.
        /// </summary>
        public int? MaxPortProbes { get; set; }

        /// <summary>
        /// Clone options
        /// </summary>
        /// <returns></returns>
        internal OpcUaDiscoveryOptions Clone() => new OpcUaDiscoveryOptions {
            Mode = Mode,
            MaxDegreeOfParallelism = MaxDegreeOfParallelism,
            AddressRanges = AddressRanges,
            PortRanges = PortRanges,
            PortProbeTimeout = PortProbeTimeout,
            NetworkProbeTimeout = NetworkProbeTimeout,
            MaxPortProbes = MaxPortProbes,
            MaxNetworkProbes = MaxNetworkProbes,
            DiscoveryIdleTime = DiscoveryIdleTime
        };

        /// <summary>
        /// Update ranges on option
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        internal bool UpdateFromModel(DiscoveryConfigModel configuration) {

            DiscoveryIdleTime = configuration?.IdleTimeBetweenScans;

            var restart =
                PortProbeTimeout != configuration?.PortProbeTimeout ||
                NetworkProbeTimeout != configuration?.NetworkProbeTimeout ||
                MaxPortProbes != configuration?.MaxPortProbes ||
                MaxNetworkProbes != configuration?.MaxNetworkProbes;

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

            PortProbeTimeout = configuration?.PortProbeTimeout;
            NetworkProbeTimeout = configuration?.NetworkProbeTimeout;
            MaxPortProbes = configuration?.MaxPortProbes;
            MaxNetworkProbes = configuration?.MaxNetworkProbes;

            return restart;
        }
    }
}
