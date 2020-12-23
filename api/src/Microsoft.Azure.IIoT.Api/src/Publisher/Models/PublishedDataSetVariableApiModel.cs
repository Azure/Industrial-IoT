// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System.Runtime.Serialization;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Dataset Field model
    /// </summary>
    [DataContract]
    public class PublishedDataSetVariableApiModel {

        /// <summary>
        /// Identifier of variable in the dataset.
        /// </summary>
        [DataMember(Name = "id", Order = 0,
            EmitDefaultValue = false)]
        public string Id { get; set; }

        /// <summary>
        /// Node id
        /// </summary>
        [DataMember(Name = "publishedVariableNodeId", Order = 1,
            EmitDefaultValue = false)]
        public string PublishedVariableNodeId { get; set; }

        /// <summary>
        /// Display name of the published node id
        /// </summary>
        [DataMember(Name = "publishedVariableDisplayName", Order = 2,
            EmitDefaultValue = false)]
        public string PublishedVariableDisplayName { get; set; }

        /// <summary>
        /// An optional component path from the node identified by
        /// PublishedVariableNodeId to the actual node to publish
        /// (Publisher extension).
        /// </summary>
        [DataMember(Name = "browsePath", Order = 3,
            EmitDefaultValue = false)]
        public string[] BrowsePath { get; set; }

        /// <summary>
        /// Default is <see cref="NodeAttribute.Value"/>.
        /// </summary>
        [DataMember(Name = "attribute", Order = 4,
            EmitDefaultValue = false)]
        public NodeAttribute? Attribute { get; set; }

        /// <summary>
        /// Index range
        /// </summary>
        [DataMember(Name = "indexRange", Order = 5,
            EmitDefaultValue = false)]
        public string IndexRange { get; set; }

        /// <summary>
        /// Sampling Interval - default is best effort
        /// </summary>
        [DataMember(Name = "samplingInterval", Order = 6,
            EmitDefaultValue = false)]
        public TimeSpan? SamplingInterval { get; set; }

        /// <summary>
        /// Data change filter
        /// </summary>
        [DataMember(Name = "dataChangeFilter", Order = 7,
            EmitDefaultValue = false)]
        public DataChangeTriggerType? DataChangeFilter { get; set; }

        /// <summary>
        /// Deadband type
        /// </summary>
        [DataMember(Name = "deadbandType", Order = 8,
            EmitDefaultValue = false)]
        public DeadbandType? DeadbandType { get; set; }

        /// <summary>
        /// Deadband value
        /// </summary>
        [DataMember(Name = "deadbandValue", Order = 9,
            EmitDefaultValue = false)]
        public double? DeadbandValue { get; set; }

        /// <summary>
        /// Substitution value for empty results
        /// </summary>
        [DataMember(Name = "substituteValue", Order = 10,
            EmitDefaultValue = false)]
        public VariantValue SubstituteValue { get; set; }

        /// <summary>
        /// MetaData properties qualified names.
        /// </summary>
        [DataMember(Name = "metaDataProperties", Order = 11,
            EmitDefaultValue = false)]
        public List<string> MetaDataProperties { get; set; }

        /// <summary>
        /// Monitoring mode (Publisher extension)
        /// </summary>
        [DataMember(Name = "monitoringMode", Order = 12,
            EmitDefaultValue = false)]
        public MonitoringMode? MonitoringMode { get; set; }

        /// <summary>
        /// Queue size (Publisher extension)
        /// </summary>
        [DataMember(Name = "queueSize", Order = 13,
            EmitDefaultValue = false)]
        public uint? QueueSize { get; set; }

        /// <summary>
        /// Discard new values if queue is full (Publisher extension)
        /// </summary>
        [DataMember(Name = "discardNew", Order = 14,
            EmitDefaultValue = false)]
        public bool? DiscardNew { get; set; }

        /// <summary>
        /// Node in dataset writer that triggers reporting
        /// (Publisher extension)
        /// </summary>
        [DataMember(Name = "triggerId", Order = 15,
            EmitDefaultValue = false)]
        public string TriggerId { get; set; }

        /// <summary>
        /// Heartbeat Interval (Publisher extension)
        /// </summary>
        [DataMember(Name = "heartbeatInterval", Order = 16,
            EmitDefaultValue = false)]
        public TimeSpan? HeartbeatInterval { get; set; }
    }
}