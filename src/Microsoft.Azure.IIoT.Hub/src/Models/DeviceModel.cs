// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Model of device registry document
    /// </summary>
    public class DeviceModel {

        /// <summary>
        /// Etag for comparison
        /// </summary>
        [JsonProperty(PropertyName = "etag")]
        public string Etag { get; set; }

        /// <summary>
        /// Device id
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Module id
        /// </summary>
        [JsonProperty(PropertyName = "moduleId")]
        public string ModuleId { get; set; }

        /// <summary>
        /// Authentication information
        /// </summary>
        [JsonProperty(PropertyName = "authentication")]
        public DeviceAuthenticationModel Authentication { get; set; }

        /// <summary>
        /// Whether device is enabled
        /// </summary>
        [JsonProperty(PropertyName = "enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        /// Whether device is connected
        /// </summary>
        [JsonProperty(PropertyName = "connected")]
        public bool Connected { get; set; }
    }
}
