// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Status response model for webservice api
    /// </summary>
    public class StatusResponseApiModel {
        /// <summary>
        /// Name of this service
        /// </summary>
        [JsonProperty(PropertyName = "Name", Order = 10)]
        public string Name { get; set; }

        /// <summary>
        /// Operational status
        /// </summary>
        [JsonProperty(PropertyName = "Status", Order = 20)]
        public string Status { get; set; }

        /// <summary>
        /// Current time
        /// </summary>
        [JsonProperty(PropertyName = "CurrentTime", Order = 30)]
        public string CurrentTime { get; set; }

        /// <summary>
        /// Start time of service
        /// </summary>
        [JsonProperty(PropertyName = "StartTime", Order = 40)]
        public string StartTime { get; set; }

        /// <summary>
        /// Up time of service
        /// </summary>
        [JsonProperty(PropertyName = "UpTime", Order = 50)]
        public long UpTime { get; set; }
    }
}
