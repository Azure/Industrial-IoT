// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Collections.Generic;

    /// <summary>
    /// Base monitored item
    /// </summary>
    public abstract record class BaseMonitoredItemModel
    {
        /// <summary>
        /// Identifier for this monitored item
        /// Prio 1: Id = DataSetFieldId - if already configured
        /// Prio 2: Id = DataSetFieldName - if already configured
        /// Prio 3: NodeId as configured
        /// </summary>
        public string Id
        {
            get =>
                !string.IsNullOrEmpty(DataSetFieldId) ? DataSetFieldId :
                !string.IsNullOrEmpty(DataSetFieldName) ? DataSetFieldName :
                StartNodeId;
        }

        /// <summary>
        /// Data set field id
        /// </summary>
        public string? DataSetFieldId { get; init; }

        /// <summary>
        /// Display name
        /// Prio 1: DisplayName = DataSetFieldName - if already configured
        /// Prio 2: DisplayName = DataSetFieldId  - if already configured
        /// Prio 3: NodeId as configured
        /// </summary>
        public string DisplayName
        {
            get =>
                !string.IsNullOrEmpty(DataSetFieldName) ? DataSetFieldName :
                !string.IsNullOrEmpty(DataSetFieldId) ? DataSetFieldId :
                StartNodeId;
        }

        /// <summary>
        /// Data set field name
        /// </summary>
        public string? DataSetFieldName { get; set; }

        /// <summary>
        /// Fetch dataset name
        /// </summary>
        public bool? FetchDataSetFieldName { get; init; }

        /// <summary>
        /// Node id
        /// </summary>
        public required string StartNodeId { get; init; }

        /// <summary>
        /// Path from node
        /// </summary>
        public IReadOnlyList<string>? RelativePath { get; init; }

        /// <summary>
        /// Attribute
        /// </summary>
        public NodeAttribute? AttributeId { get; init; }

        /// <summary>
        /// Queue size
        /// </summary>
        public uint QueueSize { get; init; }

        /// <summary>
        /// Auto calculate queue size using publishing interval
        /// </summary>
        public bool AutoSetQueueSize { get; init; }

        /// <summary>
        /// Discard new values if queue is full
        /// </summary>
        public bool? DiscardNew { get; init; }

        /// <summary>
        /// Monitoring mode
        /// </summary>
        public MonitoringMode? MonitoringMode { get; init; }

        /// <summary>
        /// Namespace format to use
        /// </summary>
        public NamespaceFormat NamespaceFormat { get; init; }

        /// <summary>
        /// Triggered items
        /// </summary>
        public IList<BaseMonitoredItemModel>? TriggeredItems { get; init; }
    }
}
