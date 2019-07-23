// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

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
            NetworkProbeTimeout = model.NetworkProbeTimeout;
            MaxNetworkProbes = model.MaxNetworkProbes;
            PortRangesToScan = model.PortRangesToScan;
            PortProbeTimeout = PortProbeTimeout;
            MaxPortProbes = model.MaxPortProbes;
            MinPortProbesPercent = model.MinPortProbesPercent;
            IdleTimeBetweenScans = IdleTimeBetweenScans;
            Callbacks = model.Callbacks?
                .Select(c => c == null ? null : new CallbackApiModel(c))
                .ToList();
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
                NetworkProbeTimeout = NetworkProbeTimeout,
                MaxNetworkProbes = MaxNetworkProbes,
                PortRangesToScan = PortRangesToScan,
                PortProbeTimeout = PortProbeTimeout,
                MaxPortProbes = MaxPortProbes,
                MinPortProbesPercent = MinPortProbesPercent,
                IdleTimeBetweenScans = IdleTimeBetweenScans,
                ActivationFilter = ActivationFilter?.ToServiceModel(),
                Callbacks = Callbacks?
                    .Where(c => c != null)
                    .Select(c => c.ToServiceModel())
                    .ToList(),
                Locales = Locales,
                DiscoveryUrls = DiscoveryUrls,
            };
        }

        /// <summary>
        /// Address ranges to scan (null == all wired nics)
        /// </summary>
        [JsonProperty(PropertyName = "AddressRangesToScan",
            NullValueHandling = NullValueHandling.Ignore)]
        public string AddressRangesToScan { get; set; }

        /// <summary>
        /// Network probe timeout.
        /// </summary>
        [JsonProperty(PropertyName = "NetworkProbeTimeout",
            NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? NetworkProbeTimeout { get; set; }

        /// <summary>
        /// Max network probes that should ever run.
        /// </summary>
        [JsonProperty(PropertyName = "MaxNetworkProbes",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? MaxNetworkProbes { get; set; }

        /// <summary>
        /// Port ranges to scan (null == all unassigned)
        /// </summary>
        [JsonProperty(PropertyName = "PortRangesToScan",
            NullValueHandling = NullValueHandling.Ignore)]
        public string PortRangesToScan { get; set; }

        /// <summary>
        /// Port probe timeout
        /// </summary>
        [JsonProperty(PropertyName = "PortProbeTimeout",
            NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? PortProbeTimeout { get; set; }

        /// <summary>
        /// Max port probes that should ever run.
        /// </summary>
        [JsonProperty(PropertyName = "MaxPortProbes",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? MaxPortProbes { get; set; }

        /// <summary>
        /// Probes that must always be there as percent of max.
        /// </summary>
        [JsonProperty(PropertyName = "MinPortProbesPercent",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? MinPortProbesPercent { get; set; }

        /// <summary>
        /// Delay time between discovery sweeps
        /// </summary>
        [JsonProperty(PropertyName = "IdleTimeBetweenScans",
            NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? IdleTimeBetweenScans { get; set; }

        /// <summary>
        /// List of preset discovery urls to use
        /// </summary>
        [JsonProperty(PropertyName = "DiscoveryUrls",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<string> DiscoveryUrls { get; set; }

        /// <summary>
        /// List of locales to filter with during discovery
        /// </summary>
        [JsonProperty(PropertyName = "Locales",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Locales { get; set; }

        /// <summary>
        /// Callbacks to invoke once onboarding finishes
        /// </summary>
        [JsonProperty(PropertyName = "Callbacks",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<CallbackApiModel> Callbacks { get; set; }

        /// <summary>
        /// Activate all twins with this filter during onboarding.
        /// </summary>
        [JsonProperty(PropertyName = "ActivationFilter",
            NullValueHandling = NullValueHandling.Ignore)]
        public EndpointActivationFilterApiModel ActivationFilter { get; set; }
    }
}
