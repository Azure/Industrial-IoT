// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using Microsoft.Azure.IIoT.Serializers;
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Publisher monitored item sample model
    /// </summary>
    [DataContract]
    public class MonitoredItemMessageApiModel {

        /// <summary>
        /// Publisher Id
        /// </summary>
        [DataMember(Name = "publisherId", Order = 0,
            EmitDefaultValue = false)]
        public string PublisherId { get; set; }

        /// <summary>
        /// DataSetWriterId
        /// </summary>
        [DataMember(Name = "dataSetWriterId", Order = 1,
            EmitDefaultValue = false)]
        public string DataSetWriterId { get; set; }

        /// <summary>
        /// Endpoint
        /// </summary>
        [DataMember(Name = "endpointId", Order = 2,
            EmitDefaultValue = false)]
        public string EndpointId { get; set; }

        /// <summary>
        /// Node id
        /// </summary>
        [DataMember(Name = "nodeId", Order = 3)]
        public string NodeId { get; set; }

        /// <summary>
        /// Node's display name
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
        /// Type id
        /// </summary>
        [DataMember(Name = "dataType", Order = 6,
            EmitDefaultValue = false)]
        public string DataType { get; set; }

        /// <summary>
        /// Status of the value (Quality)
        /// </summary>
        [DataMember(Name = "status", Order = 7,
            EmitDefaultValue = false)]
        public string Status { get; set; }

        /// <summary>
        /// Sent time stamp
        /// </summary>
        [DataMember(Name = "timestamp", Order = 8,
            EmitDefaultValue = false)]
        public DateTime? Timestamp { get; set; }

        /// <summary>
        /// Source time stamp
        /// </summary>
        [DataMember(Name = "sourceTimestamp", Order = 10,
            EmitDefaultValue = false)]
        public DateTime? SourceTimestamp { get; set; }

        /// <summary>
        /// Source pico
        /// </summary>
        [DataMember(Name = "sourcePicoseconds", Order = 11,
            EmitDefaultValue = false)]
        public ushort? SourcePicoseconds { get; set; }

        /// <summary>
        /// Server time stamp
        /// </summary>
        [DataMember(Name = "serverTimestamp", Order = 12,
            EmitDefaultValue = false)]
        public DateTime? ServerTimestamp { get; set; }

        /// <summary>
        /// Server pico
        /// </summary>
        [DataMember(Name = "serverPicoseconds", Order = 13,
            EmitDefaultValue = false)]
        public ushort? ServerPicoseconds { get; set; }
    }
}