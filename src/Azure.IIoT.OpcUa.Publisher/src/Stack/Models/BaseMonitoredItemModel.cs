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
        /// Identifier of the item
        /// </summary>
        public required string? Id { get; init; }

        /// <summary>
        /// Specifies the order of the item
        /// </summary>
        public required int Order { get; init; }

        /// <summary>
        /// Name of the item
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Start Node id
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

        /// <summary>
        /// Opaque context which will be added to the notifications
        /// </summary>
        public object? Context { get; init; }

        /// <summary>
        /// Data set field id
        /// </summary>
        public ServiceResultModel? State { get; set; }

        /// <summary>
        /// Identifier for this monitored item
        /// Prio 1: Id = DataSetFieldId - if already configured
        /// Prio 2: Id = DataSetFieldName - if already configured
        /// Prio 3: NodeId as configured
        /// </summary>
        public string GetMonitoredItemId()
        {
            return
                !string.IsNullOrEmpty(Id) ? Id :
                !string.IsNullOrEmpty(Name) ? Name :
                StartNodeId;
        }

        /// <summary>
        /// Name of the field in the monitored item notification
        /// Prio 1: DisplayName = DataSetFieldName - if already configured
        /// Prio 2: DisplayName = DataSetFieldId  - if already configured
        /// Prio 3: NodeId as configured
        /// </summary>
        public string GetMonitoredItemName()
        {
            return
                !string.IsNullOrEmpty(Name) ? Name :
                !string.IsNullOrEmpty(Id) ? Id :
                StartNodeId;
        }
    }
}
