// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.WebService.v1.Models {
    using Microsoft.Azure.IIoT.OpcTwin.Services.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Supervisor configuration
    /// </summary>
    public class DiscoveryConfigApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public DiscoveryConfigApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public DiscoveryConfigApiModel(DiscoveryConfigModel model) {
            AddressRangesToScan = model.AddressRangesToScan;
            NetworkProbeTimeoutMs = (int?)model.NetworkProbeTimeout?.TotalMilliseconds;
            MaxNetworkProbes = model.MaxNetworkProbes;
            PortRangesToScan = model.PortRangesToScan;
            PortProbeTimeoutMs = (int?)model.PortProbeTimeout?.TotalMilliseconds;
            MaxPortProbes = model.MaxPortProbes;
            IdleTimeBetweenScansSec = (int?)model.IdleTimeBetweenScans?.TotalSeconds;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public DiscoveryConfigModel ToServiceModel() {
            return new DiscoveryConfigModel {
                AddressRangesToScan = AddressRangesToScan,
                NetworkProbeTimeout = NetworkProbeTimeoutMs == null ?
                    (TimeSpan?)null : TimeSpan.FromMilliseconds((double)NetworkProbeTimeoutMs),
                MaxNetworkProbes = MaxNetworkProbes,
                PortRangesToScan = PortRangesToScan,
                PortProbeTimeout = PortProbeTimeoutMs == null ?
                    (TimeSpan?)null : TimeSpan.FromMilliseconds((double)PortProbeTimeoutMs),
                MaxPortProbes = MaxPortProbes,
                IdleTimeBetweenScans = IdleTimeBetweenScansSec == null ?
                    (TimeSpan?)null: TimeSpan.FromSeconds((double)IdleTimeBetweenScansSec)
            };
        }

        /// <summary>
        /// Address ranges to scan (null == all wired nics)
        /// </summary>
        [JsonProperty(PropertyName = "addressRangesToScan",
            NullValueHandling = NullValueHandling.Ignore)]
        public string AddressRangesToScan { get; set; }

        /// <summary>
        /// Networking probe timeout
        /// </summary>
        [JsonProperty(PropertyName = "networkProbeTimeoutMs",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? NetworkProbeTimeoutMs { get; set; }

        /// <summary>
        /// Max network probes that should ever run.
        /// </summary>
        [JsonProperty(PropertyName = "maxNetworkProbes",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? MaxNetworkProbes { get; set; }

        /// <summary>
        /// Port ranges to scan (null == all unassigned)
        /// </summary>
        [JsonProperty(PropertyName = "portRangesToScan",
            NullValueHandling = NullValueHandling.Ignore)]
        public string PortRangesToScan { get; set; }

        /// <summary>
        /// Port probe timeout
        /// </summary>
        [JsonProperty(PropertyName = "portProbeTimeoutMs",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? PortProbeTimeoutMs { get; set; }

        /// <summary>
        /// Max port probes that should ever run.
        /// </summary>
        [JsonProperty(PropertyName = "maxPortProbes",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? MaxPortProbes { get; set; }

        /// <summary>
        /// Delay time between discovery sweeps in seconds
        /// </summary>
        [JsonProperty(PropertyName = "idleTimeBetweenScansSec",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? IdleTimeBetweenScansSec { get; set; }
    }
}
