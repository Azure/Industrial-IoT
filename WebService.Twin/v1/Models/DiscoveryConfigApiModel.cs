// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.v1.Models {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
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
            MinNetworkProbes = model.MinNetworkProbes;
            MaxNetworkProbes = model.MaxNetworkProbes;
            PortRangesToScan = model.PortRangesToScan;
            MinPortProbes = model.MinPortProbes;
            MaxPortProbes = model.MaxPortProbes;
            IdleTimeBetweenScans = (int?)model.IdleTimeBetweenScans?.TotalSeconds;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public DiscoveryConfigModel ToServiceModel() {
            return new DiscoveryConfigModel {
                AddressRangesToScan = AddressRangesToScan,
                MinNetworkProbes = MinNetworkProbes,
                MaxNetworkProbes = MaxNetworkProbes,
                PortRangesToScan = PortRangesToScan,
                MinPortProbes = MinPortProbes,
                MaxPortProbes = MaxPortProbes,
                IdleTimeBetweenScans = IdleTimeBetweenScans == null ?
                    (TimeSpan?)null: TimeSpan.FromSeconds((double)IdleTimeBetweenScans)
            };
        }

        /// <summary>
        /// Address ranges to scan (null == all wired nics)
        /// </summary>
        [JsonProperty(PropertyName = "addressRangesToScan",
            NullValueHandling = NullValueHandling.Ignore)]
        public string AddressRangesToScan { get; set; }

        /// <summary>
        /// Minimum network probes that should run.
        /// </summary>
        [JsonProperty(PropertyName = "minNetworkProbes",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? MinNetworkProbes { get; set; }

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
        /// Minimum port probes that should run.
        /// </summary>
        [JsonProperty(PropertyName = "minPortProbes",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? MinPortProbes { get; set; }

        /// <summary>
        /// Max port probes that should ever run.
        /// </summary>
        [JsonProperty(PropertyName = "maxPortProbes",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? MaxPortProbes { get; set; }

        /// <summary>
        /// Delay time between discovery sweeps in seconds
        /// </summary>
        [JsonProperty(PropertyName = "idleTimeBetweenScans",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? IdleTimeBetweenScans { get; set; }
    }
}
