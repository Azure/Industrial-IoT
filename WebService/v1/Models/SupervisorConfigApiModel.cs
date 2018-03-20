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
    public class SupervisorConfigApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public SupervisorConfigApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public SupervisorConfigApiModel(SupervisorConfigModel model) {
            AddressRangesToScan = model.AddressRangesToScan;
            PortRangesToScan = model.PortRangesToScan;
            IdleTimeBetweenScans = (int?)model.IdleTimeBetweenScans?.TotalSeconds;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public SupervisorConfigModel ToServiceModel() {
            return new SupervisorConfigModel {
                AddressRangesToScan = AddressRangesToScan,
                PortRangesToScan = PortRangesToScan,
                IdleTimeBetweenScans = IdleTimeBetweenScans == null ? 
                    (TimeSpan?)null: TimeSpan.FromSeconds((double)IdleTimeBetweenScans)
            };
        }

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
