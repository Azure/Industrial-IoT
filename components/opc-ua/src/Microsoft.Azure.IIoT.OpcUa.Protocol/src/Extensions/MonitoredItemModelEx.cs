// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Collections.Generic;

    /// <summary>
    /// Monitored item model extensions
    /// </summary>
    public static class MonitoredItemModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static MonitoredItemModel Clone(this MonitoredItemModel model) {
            if (model == null) {
                return null;
            }
            return new MonitoredItemModel {
                Id = model.Id,
                TriggerId = model.TriggerId,
                StartNodeId = model.StartNodeId,
                SamplingInterval = model.SamplingInterval,
                QueueSize = model.QueueSize,
                DiscardNew = model.DiscardNew,
                DataChangeFilter = model.DataChangeFilter.Clone(),
                EventFilter = model.EventFilter.Clone(),
                AggregateFilter = model.AggregateFilter.Clone(),
                AttributeId = model.AttributeId,
                IndexRange = model.IndexRange,
                MonitoringMode = model.MonitoringMode,
                DisplayName = model.DisplayName,
                RelativePath = model.RelativePath,
                HeartbeatInterval = model.HeartbeatInterval
            };
        }

        /// <summary>
        /// Compare items
        /// </summary>
        /// <param name="model"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool IsSameAs(this MonitoredItemModel model, MonitoredItemModel other) {
            if (model == null && other == null) {
                return true;
            }
            if (model == null || other == null) {
                return false;
            }
            if (model.TriggerId != other.TriggerId) {
                return false;
            }
            if (model.StartNodeId != other.StartNodeId) {
                return false;
            }
            if (model.SamplingInterval != other.SamplingInterval) {
                return false;
            }
            if (model.QueueSize != other.QueueSize) {
                return false;
            }
            if (model.DiscardNew != other.DiscardNew) {
                return false;
            }
            if (!model.DataChangeFilter.IsSameAs(other.DataChangeFilter)) {
                return false;
            }
            if (!model.AggregateFilter.IsSameAs(other.AggregateFilter)) {
                return false;
            }
            if (!model.EventFilter.IsSameAs(other.EventFilter)) {
                return false;
            }
            if (model.AttributeId != other.AttributeId) {
                return false;
            }
            if (model.IndexRange != other.IndexRange) {
                return false;
            }
            if (model.MonitoringMode != other.MonitoringMode) {
                return false;
            }
            if (model.DisplayName != other.DisplayName) {
                return false;
            }
            if (!model.RelativePath.SequenceEqualsSafe(other.RelativePath)) {
                return false;
            }
            if (model.HeartbeatInterval != other.HeartbeatInterval) {
                return false;
            }
            return true;
        }
    }
}