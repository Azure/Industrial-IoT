// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Subscriber.Models {
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Publisher datsa set message model
    /// </summary>
    [JsonObject(Id = "dataSetMessage",
        ItemNullValueHandling = NullValueHandling.Ignore)]
    public class DataSetMessageModel {

        /// <summary>
        /// messageId - from the network message
        /// </summary>
        [JsonProperty(PropertyName = "messageId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string MessageId { get; set; }

        /// <summary>
        /// Publisher Id - from network message
        /// </summary>
        [JsonProperty(PropertyName = "publisherId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string PublisherId { get; set; }

        /// <summary>
        /// Dataset Class ID - from network message
        /// </summary>
        [JsonProperty(PropertyName = "dataSetClassId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string DataSetClassId { get; set; }

        /// <summary>
        /// Subscription id
        /// </summary>
        [JsonProperty(PropertyName = "dataSetWriterId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string DataSetWriterId { get; set; }

        /// <summary>
        /// Endpoint
        /// </summary>
        [JsonProperty(PropertyName = "sequenceNumber",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint SequenceNumber { get; set; }

        /// <summary>
        /// Endpoint
        /// </summary>
        [JsonProperty(PropertyName = "metaDataVersion",
            NullValueHandling = NullValueHandling.Ignore)]
        public string MetaDataVersion { get; set; }

        /// <summary>
        /// Status of the payload (Quality)
        /// </summary>
        [JsonProperty(PropertyName = "status",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Status { get; set; }

        /// <summary>
        /// Time stamp of the dataset
        /// </summary>
        [JsonProperty(PropertyName = "timestamp",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? Timestamp { get; set; }

        /// <summary>
        /// Dataset's payload dictionary
        /// </summary>
        [JsonProperty(PropertyName = "payload",
            NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, DataValueModel> Payload { get; set; }
    }
}