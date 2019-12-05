// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.Onboarding.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Discovery configuration
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
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            AddressRangesToScan = model.AddressRangesToScan;
            NetworkProbeTimeoutMs = (int?)model.NetworkProbeTimeout?.TotalMilliseconds;
            MaxNetworkProbes = model.MaxNetworkProbes;
            PortRangesToScan = model.PortRangesToScan;
            PortProbeTimeoutMs = (int?)model.PortProbeTimeout?.TotalMilliseconds;
            MaxPortProbes = model.MaxPortProbes;
            MinPortProbesPercent = model.MinPortProbesPercent;
            IdleTimeBetweenScansSec = (int?)model.IdleTimeBetweenScans?.TotalSeconds;
            DiscoveryUrls = model.DiscoveryUrls;
            Locales = model.Locales;
            ActivationFilter = model.ActivationFilter == null ? null :
               new EndpointActivationFilterApiModel(model.ActivationFilter);
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
                MinPortProbesPercent = MinPortProbesPercent,
                IdleTimeBetweenScans = IdleTimeBetweenScansSec == null ?
                    (TimeSpan?)null : TimeSpan.FromSeconds((double)IdleTimeBetweenScansSec),
                ActivationFilter = ActivationFilter?.ToServiceModel(),
                Locales = Locales,
                DiscoveryUrls = DiscoveryUrls
            };
        }

        /// <summary>
        /// Address ranges to scan (null == all wired nics)
        /// </summary>
        [JsonProperty(PropertyName = "addressRangesToScan",
            NullValueHandling = NullValueHandling.Ignore)]
        public string AddressRangesToScan { get; set; }

        /// <summary>
        /// Network probe timeout
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
        /// Probes that must always be there as percent of max.
        /// </summary>
        [JsonProperty(PropertyName = "minPortProbesPercent",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? MinPortProbesPercent { get; set; }

        /// <summary>
        /// Delay time between discovery sweeps in seconds
        /// </summary>
        [JsonProperty(PropertyName = "idleTimeBetweenScansSec",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? IdleTimeBetweenScansSec { get; set; }

        /// <summary>
        /// List of preset discovery urls to use
        /// </summary>
        [JsonProperty(PropertyName = "discoveryUrls",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<string> DiscoveryUrls { get; set; }

        /// <summary>
        /// List of locales to filter with during discovery
        /// </summary>
        [JsonProperty(PropertyName = "locales",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Locales { get; set; }

        /// <summary>
        /// Activate all twins with this filter during onboarding.
        /// </summary>
        [JsonProperty(PropertyName = "activationFilter",
            NullValueHandling = NullValueHandling.Ignore)]
        public EndpointActivationFilterApiModel ActivationFilter { get; set; }
    }
}
