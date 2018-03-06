// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.External.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Model of device registry document
    /// </summary>
    public class DeviceModel {

        /// <summary>
        /// Etag for comparison
        /// </summary>
        [JsonProperty(PropertyName = "Etag")]
        public string Etag { get; set; }

        /// <summary>
        /// Device id
        /// </summary>
        [JsonProperty(PropertyName = "Id")]
        public string Id { get; set; }

        /// <summary>
        /// Authentication information
        /// </summary>
        [JsonProperty(PropertyName = "Authentication")]
        public DeviceAuthenticationModel Authentication { get; set; }

        /// <summary>
        /// Host name
        /// </summary>
        [JsonProperty(PropertyName = "IoTHubHostName")]
        public string IoTHubHostName { get; set; }

        /// <summary>
        /// Whether device is enabled
        /// </summary>
        [JsonProperty(PropertyName = "Enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        /// Whether device is connected
        /// </summary>
        [JsonProperty(PropertyName = "Connected")]
        public bool Connected { get; set; }
    }
}
