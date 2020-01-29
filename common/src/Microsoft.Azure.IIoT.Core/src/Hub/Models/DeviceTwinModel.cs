// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Model of device registry / twin document
    /// </summary>
    public class DeviceTwinModel {

        /// <summary>
        /// Device id
        /// </summary>
        [JsonProperty(PropertyName = "deviceId")]
        public string Id { get; set; }

        /// <summary>
        /// Module id
        /// </summary>
        [JsonProperty(PropertyName = "moduleId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ModuleId { get; set; }

        /// <summary>
        /// Etag for comparison
        /// </summary>
        [JsonProperty(PropertyName = "etag",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Etag { get; set; }

        /// <summary>
        /// Tags
        /// </summary>
        [JsonProperty(PropertyName = "tags",
            NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, JToken> Tags { get; set; }

        /// <summary>
        /// Settings
        /// </summary>
        [JsonProperty(PropertyName = "properties",
            NullValueHandling = NullValueHandling.Ignore)]
        public TwinPropertiesModel Properties { get; set; }

        /// <summary>
        /// Capabilities
        /// </summary>
        [JsonProperty(PropertyName = "capabilities",
            NullValueHandling = NullValueHandling.Ignore)]
        public DeviceCapabilitiesModel Capabilities { get; set; }

        /// <summary>
        /// Twin's Version
        /// </summary>
        [JsonProperty(PropertyName = "version",
            NullValueHandling = NullValueHandling.Ignore)]
        public long? Version { get; set; }

        /// <summary>
        /// Gets the corresponding Device's Status.
        /// </summary>
        [JsonProperty(PropertyName = "status",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Status { get; set; }

        /// <summary>
        /// Reason, if any, for the corresponding Device
        /// to be in specified <see cref="Status"/>
        /// </summary>
        [JsonProperty(PropertyName = "statusReason",
            NullValueHandling = NullValueHandling.Ignore)]
        public string StatusReason { get; set; }

        /// <summary>
        /// Time when the corresponding Device's
        /// <see cref="Status"/> was last updated
        /// </summary>
        [JsonProperty(PropertyName = "statusUpdatedTime",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? StatusUpdatedTime { get; set; }

        /// <summary>
        /// Corresponding Device's ConnectionState
        /// </summary>
        [JsonProperty(PropertyName = "connectionState",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ConnectionState { get; set; }

        /// <summary>
        /// Time when the corresponding Device was last active
        /// </summary>
        [JsonProperty(PropertyName = "lastActivityTime",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? LastActivityTime { get; set; }
    }
}
