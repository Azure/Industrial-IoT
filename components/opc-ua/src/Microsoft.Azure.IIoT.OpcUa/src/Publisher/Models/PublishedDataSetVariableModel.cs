// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A published variable
    /// </summary>
    public class PublishedDataSetVariableModel {

        /// <summary>
        /// Identifier of variable in the dataset.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Node id of the variable
        /// </summary>
        public string PublishedVariableNodeId { get; set; }

        /// <summary>
        /// Display name of the published variable
        /// </summary>
        public string PublishedVariableDisplayName { get; set; }

        /// <summary>
        /// An optional component path from the node identified by
        /// PublishedVariableNodeId to the actual node to publish
        /// (Publisher extension).
        /// </summary>
        public string[] BrowsePath { get; set; }

        /// <summary>
        /// Default is <see cref="NodeAttribute.Value"/>.
        /// </summary>
        public NodeAttribute? Attribute { get; set; }

        /// <summary>
        /// Index range
        /// </summary>
        public string IndexRange { get; set; }

        /// <summary>
        /// Sampling Interval hint - default is best effort
        /// </summary>
        public TimeSpan? SamplingInterval { get; set; }

        /// <summary>
        /// Data change filter
        /// </summary>
        public DataChangeTriggerType? DataChangeFilter { get; set; }

        /// <summary>
        /// Deadband type
        /// </summary>
        public DeadbandType? DeadbandType { get; set; }

        /// <summary>
        /// Deadband value
        /// </summary>
        public double? DeadbandValue { get; set; }

        /// <summary>
        /// Substitution value for bad / empty results (not supported yet)
        /// </summary>
        public VariantValue SubstituteValue { get; set; }

        /// <summary>
        /// MetaData properties qualified names. (not supported yet)
        /// </summary>
        public List<string> MetaDataProperties { get; set; }

        /// <summary>
        /// Monitoring mode (Publisher extension)
        /// </summary>
        public MonitoringMode? MonitoringMode { get; set; }

        /// <summary>
        /// Queue size (Publisher extension)
        /// </summary>
        public uint? QueueSize { get; set; }

        /// <summary>
        /// Discard new values if queue is full (Publisher extension)
        /// </summary>
        public bool? DiscardNew { get; set; }

        /// <summary>
        /// Node in dataset writer that triggers reporting
        /// (Publisher extension)
        /// </summary>
        public string TriggerId { get; set; }

        /// <summary>
        /// Hidden trigger that triggers reporting this variable on
        /// at a minimum interval.  Mutually exclusive with TriggerId.
        /// (Publisher extension)
        /// </summary>
        public TimeSpan? HeartbeatInterval { get; set; }
    }
}