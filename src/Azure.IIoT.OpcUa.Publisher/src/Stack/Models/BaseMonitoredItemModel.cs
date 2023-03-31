// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Base monitored item
    /// </summary>
    public abstract record class BaseMonitoredItemModel
    {
        /// <summary>
        /// Identifier for this monitored item
        /// Prio 1: Id = DataSetFieldId - if already configured
        /// Prio 2: Id = DisplayName - if already configured
        /// Prio 3: NodeId as configured
        /// </summary>
        public string Id
        {
            get =>
                !string.IsNullOrEmpty(_id) ? _id :
                !string.IsNullOrEmpty(DisplayName) ? DisplayName :
                StartNodeId;
            init => _id = value;
        }
        private string? _id;

        /// <summary>
        /// Display name
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Node id
        /// </summary>
        public string StartNodeId { get; init; } = null!;

        /// <summary>
        /// Path from node
        /// </summary>
        public IReadOnlyList<string>? RelativePath { get; init; }

        /// <summary>
        /// Attribute
        /// </summary>
        public NodeAttribute? AttributeId { get; init; }

        /// <summary>
        /// Range of value to report
        /// </summary>
        public string? IndexRange { get; init; }

        /// <summary>
        /// Sampling interval
        /// </summary>
        public TimeSpan? SamplingInterval { get; init; }

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
    }
}
