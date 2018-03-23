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
        /// Address ranges to use or null to use from network info
        /// </summary>
        public IEnumerable<AddressRange> AddressRanges { get; set; }

        /// <summary>
        /// Network class
        /// </summary>
        public NetworkClass NetworkClass { get; set; } = NetworkClass.Wired;

        /// <summary>
        /// Port ranges to use if not from discovery mode
        /// </summary>
        public IEnumerable<PortRange> PortRanges { get; set; }


        /// <summary>
        /// Clone options
        /// </summary>
        /// <returns></returns>
        internal OpcUaDiscoveryOptions Clone() => new OpcUaDiscoveryOptions {
            Mode = Mode,
            MaxDegreeOfParallelism = MaxDegreeOfParallelism,
            AddressRanges = AddressRanges,
            PortRanges = PortRanges
        };

        /// <summary>
        /// Update ranges on option
        /// </summary>
        /// <param name="addressRanges"></param>
        /// <param name="portRanges"></param>
        /// <returns></returns>
        internal bool Update(string addressRanges, string portRanges) {
            var restart = false;
            if (!string.IsNullOrEmpty(addressRanges)) {
                if (AddressRange.TryParse(addressRanges, out var addresses)) {
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

            if (!string.IsNullOrEmpty(portRanges)) {
                if (PortRange.TryParse(portRanges, out var ports)) {
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
            return restart;
        }
    }
}
