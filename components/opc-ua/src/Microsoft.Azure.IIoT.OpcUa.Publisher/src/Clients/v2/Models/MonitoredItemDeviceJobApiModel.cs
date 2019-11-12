// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Clients.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Job configuration for monitored item jobs
    /// </summary>
    public class MonitoredItemDeviceJobApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public MonitoredItemDeviceJobApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public MonitoredItemDeviceJobApiModel(MonitoredItemDeviceJobModel model) {
            if (model?.Job == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Job = new MonitoredItemJobApiModel(model.Job);
            ConnectionString = model.ConnectionString;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public MonitoredItemDeviceJobModel ToServiceModel() {
            return new MonitoredItemDeviceJobModel {
                Job = Job?.ToServiceModel(),
                ConnectionString = ConnectionString
            };
        }

        /// <summary>
        /// Monitored item job
        /// </summary>
        [JsonProperty(PropertyName = "job")]
        public MonitoredItemJobApiModel Job { get; set; }

        /// <summary>
        /// Connection string
        /// </summary>
        [JsonProperty(PropertyName = "connectionString")]
        public string ConnectionString { get; set; }
    }
}