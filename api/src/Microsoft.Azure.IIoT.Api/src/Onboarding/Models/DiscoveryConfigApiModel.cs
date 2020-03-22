// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Onboarding.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Discovery configuration
    /// </summary>
    [DataContract]
    public class DiscoveryConfigApiModel {

        /// <summary>
        /// Address ranges to scan (null == all wired nics)
        /// </summary>
        [DataMember(Name = "addressRangesToScan",
            EmitDefaultValue = false)]
        public string AddressRangesToScan { get; set; }

        /// <summary>
        /// Network probe timeout
        /// </summary>
        [DataMember(Name = "networkProbeTimeoutMs",
            EmitDefaultValue = false)]
        public int? NetworkProbeTimeoutMs { get; set; }

        /// <summary>
        /// Max network probes that should ever run.
        /// </summary>
        [DataMember(Name = "maxNetworkProbes",
            EmitDefaultValue = false)]
        public int? MaxNetworkProbes { get; set; }

        /// <summary>
        /// Port ranges to scan (null == all unassigned)
        /// </summary>
        [DataMember(Name = "portRangesToScan",
            EmitDefaultValue = false)]
        public string PortRangesToScan { get; set; }

        /// <summary>
        /// Port probe timeout
        /// </summary>
        [DataMember(Name = "portProbeTimeoutMs",
            EmitDefaultValue = false)]
        public int? PortProbeTimeoutMs { get; set; }

        /// <summary>
        /// Max port probes that should ever run.
        /// </summary>
        [DataMember(Name = "maxPortProbes",
            EmitDefaultValue = false)]
        public int? MaxPortProbes { get; set; }

        /// <summary>
        /// Probes that must always be there as percent of max.
        /// </summary>
        [DataMember(Name = "minPortProbesPercent",
            EmitDefaultValue = false)]
        public int? MinPortProbesPercent { get; set; }

        /// <summary>
        /// Delay time between discovery sweeps in seconds
        /// </summary>
        [DataMember(Name = "idleTimeBetweenScansSec",
            EmitDefaultValue = false)]
        public int? IdleTimeBetweenScansSec { get; set; }

        /// <summary>
        /// List of preset discovery urls to use
        /// </summary>
        [DataMember(Name = "discoveryUrls",
            EmitDefaultValue = false)]
        public List<string> DiscoveryUrls { get; set; }

        /// <summary>
        /// List of locales to filter with during discovery
        /// </summary>
        [DataMember(Name = "locales",
            EmitDefaultValue = false)]
        public List<string> Locales { get; set; }

        /// <summary>
        /// Activate all twins with this filter during onboarding.
        /// </summary>
        [DataMember(Name = "activationFilter",
            EmitDefaultValue = false)]
        public EndpointActivationFilterApiModel ActivationFilter { get; set; }
    }
}
