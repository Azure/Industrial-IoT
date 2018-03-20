// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client.Models {
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Discovery request api model
    /// </summary>
    public class SupervisorConfigApiModel {
        /// <summary>
        /// Address ranges to scan (null == all wired nics)
        /// </summary>
        [JsonProperty(PropertyName = "addressesRangesToScan",
            NullValueHandling = NullValueHandling.Ignore)]
        public string AddressRangesToScan { get; set; }

        /// <summary>
        /// Port ranges to scan (null == all unassigned)
        /// </summary>
        [JsonProperty(PropertyName = "portRangesToScan",
            NullValueHandling = NullValueHandling.Ignore)]
        public string PortRangesToScan { get; set; }

        /// <summary>
        /// Delay time between discovery sweeps in seconds
        /// </summary>
        [JsonProperty(PropertyName = "idleTimeBetweenScans",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? IdleTimeBetweenScans { get; set; } 
    }
}
