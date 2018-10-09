// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Runtime {
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// IoT hub services runtime configuration
    /// </summary>
    public class IoTHubConfig : ConfigBase, IIoTHubConfig {

        /// <summary>
        /// Service configuration
        /// </summary>
        private const string kIoTHubConnectionStringKey = "IoTHubConnectionString";

        /// <summary>IoT hub connection string</summary>
        public string IoTHubConnString => GetStringOrDefault(kIoTHubConnectionStringKey,
            GetStringOrDefault(_serviceId + "_HUB_CS",
                GetStringOrDefault("PCS_IOTHUB_CONNSTRING", GetStringOrDefault("_HUB_CS", null))));
        /// <summary>Resource Id</summary>
        public string IoTHubResourceId { get; set; }

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="serviceId"></param>
        /// <param name="configuration"></param>
        public IoTHubConfig(IConfigurationRoot configuration, string serviceId = "") :
            base (configuration) {
            _serviceId = serviceId?.ToUpperInvariant() ??
                throw new ArgumentNullException(nameof(serviceId));
        }

        private readonly string _serviceId;
    }
}
