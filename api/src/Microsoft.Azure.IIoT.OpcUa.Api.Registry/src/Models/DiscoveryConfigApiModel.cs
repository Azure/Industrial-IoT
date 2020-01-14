// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel;

    /// <summary>
    /// Discovery configuration api model
    /// </summary>
    public class DiscoveryConfigApiModel {

        /// <summary>
        /// Address ranges to scan (null == all wired nics)
        /// </summary>
        [JsonProperty(PropertyName = "addressRangesToScan",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string AddressRangesToScan { get; set; }

        /// <summary>
        /// Network probe timeout
        /// </summary>
        [JsonProperty(PropertyName = "networkProbeTimeoutMs",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public int? NetworkProbeTimeoutMs { get; set; }

        /// <summary>
        /// Max network probes that should ever run.
        /// </summary>
        [JsonProperty(PropertyName = "maxNetworkProbes",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public int? MaxNetworkProbes { get; set; }

        /// <summary>
        /// Port ranges to scan (null == all unassigned)
        /// </summary>
        [JsonProperty(PropertyName = "portRangesToScan",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string PortRangesToScan { get; set; }

        /// <summary>
        /// Port probe timeout
        /// </summary>
        [JsonProperty(PropertyName = "portProbeTimeoutMs",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public int? PortProbeTimeoutMs { get; set; }

        /// <summary>
        /// Max port probes that should ever run.
        /// </summary>
        [JsonProperty(PropertyName = "maxPortProbes",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public int? MaxPortProbes { get; set; }

        /// <summary>
        /// Probes that must always be there as percent of max.
        /// </summary>
        [JsonProperty(PropertyName = "minPortProbesPercent",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public int? MinPortProbesPercent { get; set; }

        /// <summary>
        /// Delay time between discovery sweeps in seconds
        /// </summary>
        [JsonProperty(PropertyName = "idleTimeBetweenScansSec",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public int? IdleTimeBetweenScansSec { get; set; }

        /// <summary>
        /// List of preset discovery urls to use
        /// </summary>
        [JsonProperty(PropertyName = "discoveryUrls",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public List<string> DiscoveryUrls { get; set; }

        /// <summary>
        /// List of locales to filter with during discovery
        /// </summary>
        [JsonProperty(PropertyName = "locales",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public List<string> Locales { get; set; }

        /// <summary>
        /// Activate all twins with this filter during onboarding.
        /// </summary>
        [JsonProperty(PropertyName = "activationFilter",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public EndpointActivationFilterApiModel ActivationFilter { get; set; }
    }
}
