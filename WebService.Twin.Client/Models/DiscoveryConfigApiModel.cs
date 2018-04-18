// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Discovery request api model
    /// </summary>
    public class DiscoveryConfigApiModel {

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
