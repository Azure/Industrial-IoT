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
    public abstract record class BaseMonitoredItemModel : BaseItemModel
    {
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
        /// Triggered items
        /// </summary>
        public IList<BaseItemModel>? TriggeredItems { get; init; }

        /// <summary>
        /// Namespace format to use
        /// </summary>
        public NamespaceFormat NamespaceFormat { get; init; }
    }
}
