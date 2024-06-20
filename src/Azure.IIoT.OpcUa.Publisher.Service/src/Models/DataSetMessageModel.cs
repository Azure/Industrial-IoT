// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Subscriber
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Publisher datsa set message model
    /// </summary>
    [DataContract]
    public class DataSetMessageModel
    {
        /// <summary>
        /// messageId - from the network message
        /// </summary>
        [DataMember(Name = "messageId", Order = 0,
            EmitDefaultValue = false)]
        public string? MessageId { get; set; }

        /// <summary>
        /// Publisher Id - from network message
        /// </summary>
        [DataMember(Name = "publisherId", Order = 1,
            EmitDefaultValue = false)]
        public string? PublisherId { get; set; }

        /// <summary>
        /// Dataset Class ID - from network message
        /// </summary>
        [DataMember(Name = "dataSetClassId", Order = 2,
            EmitDefaultValue = false)]
        public string? DataSetClassId { get; set; }

        /// <summary>
        /// Data set writer name
        /// </summary>
        [DataMember(Name = "dataSetWriterId", Order = 3,
            EmitDefaultValue = false)]
        public string? DataSetWriterId { get; set; }

        /// <summary>
        /// Endpoint
        /// </summary>
        [DataMember(Name = "sequenceNumber", Order = 4,
            EmitDefaultValue = false)]
        public uint SequenceNumber { get; set; }

        /// <summary>
        /// Endpoint
        /// </summary>
        [DataMember(Name = "metaDataVersion", Order = 5,
            EmitDefaultValue = false)]
        public string? MetaDataVersion { get; set; }

        /// <summary>
        /// Status of the payload (Quality)
        /// </summary>
        [DataMember(Name = "status", Order = 6,
            EmitDefaultValue = false)]
        public string? Status { get; set; }

        /// <summary>
        /// Time stamp of the dataset
        /// </summary>
        [DataMember(Name = "timestamp", Order = 7,
            EmitDefaultValue = false)]
        public DateTimeOffset? Timestamp { get; set; }

        /// <summary>
        /// Dataset's payload dictionary
        /// </summary>
        [DataMember(Name = "payload", Order = 8,
            EmitDefaultValue = false)]
        public Dictionary<string, DataValueModel?>? Payload { get; set; }
    }
}
