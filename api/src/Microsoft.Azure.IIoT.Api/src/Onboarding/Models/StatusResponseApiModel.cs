// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Onboarding.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Status response model
    /// </summary>
    public class StatusResponseApiModel {
        /// <summary>
        /// Name of this service
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Operational status
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        /// <summary>
        /// Current time
        /// </summary>
        [JsonProperty(PropertyName = "currentTime")]
        public string CurrentTime { get; set; }

        /// <summary>
        /// Start time of service
        /// </summary>
        [JsonProperty(PropertyName = "startTime")]
        public string StartTime { get; set; }

        /// <summary>
        /// Up time of service
        /// </summary>
        [JsonProperty(PropertyName = "upTime")]
        public long UpTime { get; set; }
    }
}
