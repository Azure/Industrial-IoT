// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
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
        public uint QueueSize { get; set; }

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
        ///  Identifier to show for notification in payload of IoT Hub method
        ///  Prio 1: Id = DataSetFieldId - if already configured
        ///  Prio 2: Id = DisplayName - if already configured
        ///  Prio 3: NodeId as configured
        /// </summary>
        public string DataSetFieldName => !string.IsNullOrEmpty(Id) ?
            Id : !string.IsNullOrEmpty(DisplayName) ? DisplayName : StartNodeId;

        /// <summary>
        /// Clones this object
        /// </summary>
        /// <returns></returns>
        public abstract BaseMonitoredItemModel Clone();

        /// <summary>
        /// Equals function
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>If the objects are equal</returns>
        public override bool Equals(object obj) {
            return obj is BaseMonitoredItemModel model &&
                Id == model.Id &&
                DisplayName == model.DisplayName &&
                StartNodeId == model.StartNodeId &&
                EqualityComparer<string[]>.Default.Equals(RelativePath, model.RelativePath) &&
                AttributeId == model.AttributeId &&
                IndexRange == model.IndexRange &&
                EqualityComparer<TimeSpan?>.Default.Equals(SamplingInterval, model.SamplingInterval) &&
                QueueSize == model.QueueSize &&
                DiscardNew == model.DiscardNew &&
                MonitoringMode == model.MonitoringMode &&
                TriggerId == model.TriggerId;
        }

        /// <summary>
        /// Calculate hash code
        /// </summary>
        /// <returns>The hash code</returns>
        public override int GetHashCode() {
            var hash = new HashCode();
            hash.Add(base.GetHashCode());
            hash.Add(Id);
            hash.Add(DisplayName);
            hash.Add(StartNodeId);
            hash.Add(RelativePath);
            hash.Add(AttributeId);
            hash.Add(IndexRange);
            hash.Add(SamplingInterval);
            hash.Add(QueueSize);
            hash.Add(DiscardNew);
            hash.Add(MonitoringMode);
            hash.Add(TriggerId);
            return hash.ToHashCode();
        }
    }
}