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
        /// Subscription id
        /// </summary>
        [DataMember(Name = "subscriptionId",
            EmitDefaultValue = false)]
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Endpoint
        /// </summary>
        [DataMember(Name = "endpointId",
            EmitDefaultValue = false)]
        public string EndpointId { get; set; }

        /// <summary>
        /// Dataset id
        /// </summary>
        [DataMember(Name = "dataSetId",
            EmitDefaultValue = false)]
        public string DataSetId { get; set; }

        /// <summary>
        /// Node id
        /// </summary>
        [DataMember(Name = "nodeId")]
        public string NodeId { get; set; }

        /// <summary>
        /// Node's display name
        /// </summary>
        [DataMember(Name = "displayName",
            EmitDefaultValue = false)]
        public string DisplayName { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        [DataMember(Name = "value",
            EmitDefaultValue = false)]
        public VariantValue Value { get; set; }

        /// <summary>
        /// Type id
        /// </summary>
        [DataMember(Name = "typeId",
            EmitDefaultValue = false)]
        public string TypeId { get; set; }

        /// <summary>
        /// Status of the value (Quality)
        /// </summary>
        [DataMember(Name = "status",
            EmitDefaultValue = false)]
        public string Status { get; set; }

        /// <summary>
        /// Sent time stamp
        /// </summary>
        [DataMember(Name = "timestamp",
            EmitDefaultValue = false)]
        public DateTime? Timestamp { get; set; }

        /// <summary>
        /// Source time stamp
        /// </summary>
        [DataMember(Name = "sourceTimestamp",
            EmitDefaultValue = false)]
        public DateTime? SourceTimestamp { get; set; }

        /// <summary>
        /// Source pico
        /// </summary>
        [DataMember(Name = "sourcePicoseconds",
            EmitDefaultValue = false)]
        public ushort? SourcePicoseconds { get; set; }

        /// <summary>
        /// Server time stamp
        /// </summary>
        [DataMember(Name = "serverTimestamp",
            EmitDefaultValue = false)]
        public DateTime? ServerTimestamp { get; set; }

        /// <summary>
        /// Server pico
        /// </summary>
        [DataMember(Name = "serverPicoseconds",
            EmitDefaultValue = false)]
        public ushort? ServerPicoseconds { get; set; }
    }
}