// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System;

    /// <summary>
    /// Monitored item
    /// </summary>
    public class MonitoredItemModel {

        /// <summary>
        /// Identifier for this monitored item
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Display name
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Node id
        /// </summary>
        public string StartNodeId { get; set; }

        /// <summary>
        /// Path from node
        /// </summary>
        public string[] RelativePath { get; set; }

        /// <summary>
        /// Attribute
        /// </summary>
        public NodeAttribute? AttributeId { get; set; }

        /// <summary>
        /// Range of value to report
        /// </summary>
        public string IndexRange { get; set; }

        /// <summary>
        /// Sampling interval
        /// </summary>
        public TimeSpan? SamplingInterval { get; set; }

        /// <summary>
        /// Queue size
        /// </summary>
        public uint? QueueSize { get; set; }

        /// <summary>
        /// Discard new values if queue is full
        /// </summary>
        public bool? DiscardNew { get; set; }

        /// <summary>
        /// Monitoring mode
        /// </summary>
        public MonitoringMode? MonitoringMode { get; set; }

        /// <summary>
        /// Monitored item that triggers reporting of this item
        /// </summary>
        public string TriggerId { get; set; }

        /// <summary>
        /// Data change filter
        /// </summary>
        public DataChangeFilterModel DataChangeFilter { get; set; }

        /// <summary>
        /// Event filter
        /// </summary>
        public EventFilterModel EventFilter { get; set; }

        /// <summary>
        /// Aggregate filter
        /// </summary>
        public AggregateFilterModel AggregateFilter { get; set; }

        /// <summary>
        /// heartbeat interval not present if zero
        /// </summary>
        public TimeSpan? HeartbeatInterval { get; set; }
    }
}