// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Subscriber.Models {
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Monitored item sample message
    /// </summary>
    public class MonitoredItemMessageModel {

        /// <summary>
        /// Publisher Id
        /// </summary>
        [DataMember(Name = "publisherId", Order = 0,
            EmitDefaultValue = false)]
        public string PublisherId { get; set; }

        /// <summary>
        /// Dataset writer id
        /// </summary>
        [DataMember(Name = "dataSetWriterId", Order = 1,
            EmitDefaultValue = false)]
        public string DataSetWriterId { get; set; }

        /// <summary>
        /// Endpoint identifier
        /// </summary>
        [DataMember(Name = "endpointId", Order = 2,
            EmitDefaultValue = false)]
        public string EndpointId { get; set; }

        /// <summary>
        /// Node id
        /// </summary>
        [DataMember(Name = "nodeId", Order = 3,
            EmitDefaultValue = false)]
        public string NodeId { get; set; }

        /// <summary>
        /// Display name
        /// </summary>
        [DataMember(Name = "displayName", Order = 4,
            EmitDefaultValue = false)]
        public string DisplayName { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        [DataMember(Name = "value", Order = 5,
            EmitDefaultValue = false)]
        public VariantValue Value { get; set; }

        /// <summary>
        /// Data type
        /// </summary>
        [DataMember(Name = "dataType", Order = 6,
            EmitDefaultValue = false)]
        public string DataType { get; set; }

        /// <summary>
        /// Value's Status code string representation
        /// </summary>
        [DataMember(Name = "status", Order = 7,
            EmitDefaultValue = false)]
        public string Status { get; set; }

        /// <summary>
        /// Publisher's time stamp
        /// </summary>
        [DataMember(Name = "timestamp", Order = 8,
            EmitDefaultValue = false)]
        public DateTime? Timestamp { get; set; }

        /// <summary>
        /// Source time stamp
        /// </summary>
        [DataMember(Name = "sourceTimestamp", Order = 9,
            EmitDefaultValue = false)]
        public DateTime? SourceTimestamp { get; set; }

        /// <summary>
        /// Source pico
        /// </summary>
        [DataMember(Name = "sourcePicoseconds", Order = 10,
            EmitDefaultValue = false)]
        public ushort? SourcePicoseconds { get; set; }

        /// <summary>
        /// Server time stamp
        /// </summary>
        [DataMember(Name = "serverTimestamp", Order = 11,
            EmitDefaultValue = false)]
        public DateTime? ServerTimestamp { get; set; }

        /// <summary>
        /// Server pico
        /// </summary>
        [DataMember(Name = "serverPicoseconds", Order = 12,
            EmitDefaultValue = false)]
        public ushort? ServerPicoseconds { get; set; }

        /// <summary>
        /// Sequence Number
        /// </summary>
        [DataMember(Name = "sequenceNumber", Order = 13,
            EmitDefaultValue = false)]
        public uint? SequenceNumber { get; set; }
    }
}