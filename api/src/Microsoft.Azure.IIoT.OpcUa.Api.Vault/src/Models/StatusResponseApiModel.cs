// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Status model
    /// </summary>
    public sealed class StatusResponseApiModel {

        /// <summary>
        /// Service name
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        /// <summary>
        /// Current time
        /// </summary>
        [JsonProperty(PropertyName = "currentTime")]
        public string CurrentTime { get; set; }

        /// <summary>
        /// Service start time
        /// </summary>
        [JsonProperty(PropertyName = "startTime")]
        public string StartTime { get; set; }

        /// <summary>
        /// Uptime
        /// </summary>
        [JsonProperty(PropertyName = "upTime")]
        public long UpTime { get; set; }

        /// <summary>
        /// Value generated at bootstrap by each instance of the service and
        /// used to correlate logs coming from the same instance. The value
        /// changes every time the service starts.
        /// </summary>
        [JsonProperty(PropertyName = "uid")]
        public string UID { get; set; }

        /// <summary>
        /// A property bag with details about the service
        /// </summary>
        [JsonProperty(PropertyName = "properties")]
        public Dictionary<string, string> Properties { get; set; }

        /// <summary>
        /// A property bag with details about the internal dependencies
        /// </summary>
        [JsonProperty(PropertyName = "dependencies")]
        public Dictionary<string, string> Dependencies { get; set; }

        /// <summary>
        /// Meta data
        /// </summary>
        [JsonProperty(PropertyName = "$metadata")]
        public Dictionary<string, string> Metadata { get; set; }
    }
}
