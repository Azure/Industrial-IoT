// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService.v1.Models {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
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
            IdleTimeBetweenScans = model.IdleTimeBetweenScans;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public SupervisorConfigModel ToServiceModel() {
            return new SupervisorConfigModel {
                AddressRangesToScan = AddressRangesToScan,
                PortRangesToScan = PortRangesToScan,
                IdleTimeBetweenScans = IdleTimeBetweenScans
            };
        }

        /// <summary>
        /// Address ranges to scan (null == all wired nics)
        /// </summary>
        public string AddressRangesToScan { get; set; }

        /// <summary>
        /// Port ranges to scan (null == all unassigned)
        /// </summary>
        public string PortRangesToScan { get; set; }

        /// <summary>
        /// Delay time between discovery sweeps in seconds
        /// </summary>
        public TimeSpan? IdleTimeBetweenScans { get; set; }
    }
}
