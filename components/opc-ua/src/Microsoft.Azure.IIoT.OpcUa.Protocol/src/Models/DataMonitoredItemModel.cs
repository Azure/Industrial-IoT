// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System;

    /// <summary>
    /// Data monitored item
    /// </summary>
    public class DataMonitoredItemModel : BaseMonitoredItemModel {
        /// <summary>
        /// Data change filter
        /// </summary>
        public DataChangeFilterModel DataChangeFilter { get; set; }

        /// <summary>
        /// Aggregate filter
        /// </summary>
        public AggregateFilterModel AggregateFilter { get; set; }

        /// <summary>
        /// heartbeat interval not present if zero
        /// </summary>
        public TimeSpan? HeartbeatInterval { get; set; }

        /// <summary>
        /// Clone
        /// </summary>
        /// <returns></returns>
        public override BaseMonitoredItemModel Clone() {
            return new DataMonitoredItemModel {
                Id = Id,
                TriggerId = TriggerId,
                StartNodeId = StartNodeId,
                SamplingInterval = SamplingInterval,
                QueueSize = QueueSize,
                DiscardNew = DiscardNew,
                DataChangeFilter = DataChangeFilter.Clone(),
                AggregateFilter = AggregateFilter.Clone(),
                AttributeId = AttributeId,
                IndexRange = IndexRange,
                MonitoringMode = MonitoringMode,
                DisplayName = DisplayName,
                RelativePath = RelativePath,
                HeartbeatInterval = HeartbeatInterval
            };
        }

        /// <summary>
        /// Compare items
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public override bool IsSameAs(BaseMonitoredItemModel other) {
            var dataMonitoredItemModel = other as DataMonitoredItemModel;

            if (dataMonitoredItemModel == null) {
                return false;
            }
            if (!base.IsSameAs(other)) {
                return false;
            }
            if (!DataChangeFilter.IsSameAs(dataMonitoredItemModel.DataChangeFilter)) {
                return false;
            }
            if (!AggregateFilter.IsSameAs(dataMonitoredItemModel.AggregateFilter)) {
                return false;
            }
            if (HeartbeatInterval != dataMonitoredItemModel.HeartbeatInterval) {
                return false;
            }
            return true;
        }
    }
}
