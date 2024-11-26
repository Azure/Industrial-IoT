// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using System;

    /// <summary>
    /// Monitored item options base
    /// </summary>
    public record class MonitoredItemOptions
    {
        /// <summary>
        /// A display name for the monitored item.
        /// </summary>
        public string? DisplayName { get; init; }

        /// <summary>
        /// The start node for the browse path that
        /// identifies the node to monitor.
        /// </summary>
        public required NodeId StartNodeId { get; init; }

        /// <summary>
        /// The node class of the node being monitored
        /// (affects the type of filter available).
        /// </summary>
        public NodeClass NodeClass { get; init; }

        /// <summary>
        /// The attribute to monitor.
        /// </summary>
        public uint AttributeId { get; init; }

        /// <summary>
        /// The range of array indexes to monitor.
        /// </summary>
        public string? IndexRange { get; init; }

        /// <summary>
        /// The encoding to use when returning notifications.
        /// </summary>
        public QualifiedName? Encoding { get; init; }

        /// <summary>
        /// The monitoring mode.
        /// </summary>
        public MonitoringMode MonitoringMode { get; init; }

        /// <summary>
        /// The sampling interval.
        /// </summary>
        public TimeSpan SamplingInterval { get; init; }

        /// <summary>
        /// The filter to use to select values to return.
        /// </summary>
        public MonitoringFilter? Filter { get; init; }

        /// <summary>
        /// The length of the queue used to buffer values.
        /// </summary>
        public uint QueueSize { get; init; }

        /// <summary>
        /// Whether to discard the oldest entries in the
        /// queue when it is full.
        /// </summary>
        public bool DiscardOldest { get; init; }
    }
}
