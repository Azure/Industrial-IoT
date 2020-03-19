// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Publisher monitored item sample model
    /// </summary>
    [JsonObject(Id = "MonitoredItemMessageApiModel")]
    public class MonitoredItemMessageApiModel {

        /// <summary>
        /// Subscription id
        /// </summary>
        [JsonProperty(PropertyName = "subscriptionId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Endpoint
        /// </summary>
        [JsonProperty(PropertyName = "endpointId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string EndpointId { get; set; }

        /// <summary>
        /// Dataset id
        /// </summary>
        [JsonProperty(PropertyName = "dataSetId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string DataSetId { get; set; }

        /// <summary>
        /// Node id
        /// </summary>
        [JsonProperty(PropertyName = "nodeId")]
        public string NodeId { get; set; }

        /// <summary>
        /// Node's display name
        /// </summary>
        [JsonProperty(PropertyName = "displayName")]
        public string DisplayName { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        [JsonProperty(PropertyName = "value",
            NullValueHandling = NullValueHandling.Ignore)]
        public dynamic Value {get; set; }
        
        /// <summary>
        /// Type id
        /// </summary>
        [JsonProperty(PropertyName = "typeId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string TypeId { get; set; }

        /// <summary>
        /// Status of the value (Quality)
        /// </summary>
        [JsonProperty(PropertyName = "status",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Status { get; set; }

        /// <summary>
        /// Sent time stamp
        /// </summary>
        [JsonProperty(PropertyName = "timestamp",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? Timestamp { get; set; }

        /// <summary>
        /// Source time stamp
        /// </summary>
        [JsonProperty(PropertyName = "sourceTimestamp",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? SourceTimestamp { get; set; }

        /// <summary>
        /// Source pico
        /// </summary>
        [JsonProperty(PropertyName = "sourcePicoseconds",
            NullValueHandling = NullValueHandling.Ignore)]
        public ushort? SourcePicoseconds { get; set; }

        /// <summary>
        /// Server time stamp
        /// </summary>
        [JsonProperty(PropertyName = "serverTimestamp",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ServerTimestamp { get; set; }

        /// <summary>
        /// Server pico
        /// </summary>
        [JsonProperty(PropertyName = "serverPicoseconds",
            NullValueHandling = NullValueHandling.Ignore)]
        public ushort? ServerPicoseconds { get; set; }
    }
}