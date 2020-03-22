// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Subscriber.Models {
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Publisher datsa set message model
    /// </summary>
    [DataContract]
    public class DataSetMessageModel {

        /// <summary>
        /// messageId - from the network message
        /// </summary>
        [DataMember(Name = "messageId",
            EmitDefaultValue = false)]
        public string MessageId { get; set; }

        /// <summary>
        /// Publisher Id - from network message
        /// </summary>
        [DataMember(Name = "publisherId",
            EmitDefaultValue = false)]
        public string PublisherId { get; set; }

        /// <summary>
        /// Dataset Class ID - from network message
        /// </summary>
        [DataMember(Name = "dataSetClassId",
            EmitDefaultValue = false)]
        public string DataSetClassId { get; set; }

        /// <summary>
        /// Subscription id
        /// </summary>
        [DataMember(Name = "dataSetWriterId",
            EmitDefaultValue = false)]
        public string DataSetWriterId { get; set; }

        /// <summary>
        /// Endpoint
        /// </summary>
        [DataMember(Name = "sequenceNumber",
            EmitDefaultValue = false)]
        public uint SequenceNumber { get; set; }

        /// <summary>
        /// Endpoint
        /// </summary>
        [DataMember(Name = "metaDataVersion",
            EmitDefaultValue = false)]
        public string MetaDataVersion { get; set; }

        /// <summary>
        /// Status of the payload (Quality)
        /// </summary>
        [DataMember(Name = "status",
            EmitDefaultValue = false)]
        public string Status { get; set; }

        /// <summary>
        /// Time stamp of the dataset
        /// </summary>
        [DataMember(Name = "timestamp",
            EmitDefaultValue = false)]
        public DateTime? Timestamp { get; set; }

        /// <summary>
        /// Dataset's payload dictionary
        /// </summary>
        [DataMember(Name = "payload",
            EmitDefaultValue = false)]
        public Dictionary<string, DataValueModel> Payload { get; set; }
    }
}