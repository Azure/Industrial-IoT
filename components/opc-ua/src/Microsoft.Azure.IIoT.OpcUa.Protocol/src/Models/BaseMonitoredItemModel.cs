// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Base monitored item
    /// </summary>
    public abstract class BaseMonitoredItemModel {

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
        /// Clones this object
        /// </summary>
        /// <returns></returns>
        public abstract BaseMonitoredItemModel Clone();

        /// <summary>
        /// Compare items
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public virtual bool IsSameAs(BaseMonitoredItemModel other) {
            if (other == null) {
                return false;
            }
            if (TriggerId != other.TriggerId) {
                return false;
            }
            if (StartNodeId != other.StartNodeId) {
                return false;
            }
            if (SamplingInterval != other.SamplingInterval) {
                return false;
            }
            if (QueueSize != other.QueueSize) {
                return false;
            }
            if (DiscardNew != other.DiscardNew) {
                return false;
            }
            if (AttributeId != other.AttributeId) {
                return false;
            }
            if (IndexRange != other.IndexRange) {
                return false;
            }
            if (MonitoringMode != other.MonitoringMode) {
                return false;
            }
            if (DisplayName != other.DisplayName) {
                return false;
            }
            if (!RelativePath.SequenceEqualsSafe(other.RelativePath)) {
                return false;
            }
            return true;
        }
    }
}