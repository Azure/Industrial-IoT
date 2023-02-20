// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Shared.Models {
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// A published variable
    /// </summary>
    [DataContract]
    public sealed record class PublishedDataSetVariableModel {

        /// <summary>
        /// Name of variable in the dataset.
        /// </summary>
        [DataMember(Name = "id", Order = 0,
            EmitDefaultValue = false)]
        public string? Id { get; set; }

        /// <summary>
        /// Node id of the variable
        /// </summary>
        [DataMember(Name = "publishedVariableNodeId", Order = 1,
            EmitDefaultValue = false)]
        public string? PublishedVariableNodeId { get; set; }

        /// <summary>
        /// Display name of the published variable
        /// </summary>
        [DataMember(Name = "publishedVariableDisplayName", Order = 2,
            EmitDefaultValue = false)]
        public string? PublishedVariableDisplayName { get; set; }

        /// <summary>
        /// An optional component path from the node identified by
        /// PublishedVariableNodeId to the actual node to publish
        /// (Publisher extension).
        /// </summary>
        [DataMember(Name = "browsePath", Order = 3,
            EmitDefaultValue = false)]
        public string[]? BrowsePath { get; set; }

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
        public string? IndexRange { get; set; }

        /// <summary>
        /// Sampling Interval hint - default is best effort
        /// </summary>
        [DataMember(Name = "samplingInterval", Order = 6,
            EmitDefaultValue = false)]
        public TimeSpan? SamplingInterval { get; set; }

        /// <summary>
        /// Data change trigger
        /// </summary>
        [DataMember(Name = "dataChangeTrigger", Order = 7,
            EmitDefaultValue = false)]
        public DataChangeTriggerType? DataChangeTrigger { get; set; }

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
        /// Substitution value for bad / empty results (not supported yet)
        /// </summary>
        [DataMember(Name = "substituteValue", Order = 10,
            EmitDefaultValue = false)]
        public VariantValue? SubstituteValue { get; set; }

        /// <summary>
        /// MetaData properties qualified names. (not supported yet)
        /// </summary>
        [DataMember(Name = "metaDataProperties", Order = 11,
            EmitDefaultValue = false)]
        public List<string>? MetaDataProperties { get; set; }

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
        /// Hidden trigger that triggers reporting this variable on
        /// at a minimum interval.  Mutually exclusive with TriggerId.
        /// (Publisher extension)
        /// </summary>
        [DataMember(Name = "heartbeatInterval", Order = 16,
            EmitDefaultValue = false)]
        public TimeSpan? HeartbeatInterval { get; set; }

        /// <summary>
        /// Instruct the monitored item to skip the first received value
        /// </summary>
        [DataMember(Name = "skipFirst", Order = 17,
            EmitDefaultValue = false)]
        public bool? SkipFirst { get; set; }

        /// <summary>
        /// Identifier of field in the dataset class.
        /// </summary>
        [DataMember(Name = "dataSetClassFieldId", Order = 18,
            EmitDefaultValue = false)]
        public Guid DataSetClassFieldId { get; set; }
    }
}