// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Data monitored item
    /// </summary>
    public class DataMonitoredItemModel : BaseMonitoredItemModel {

        /// <summary>
        /// Field id in class
        /// </summary>
        public Guid DataSetClassFieldId { get; set; }

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
        /// Skip first value
        /// </summary>
        public bool SkipFirst { get; set; }

        /// <summary>
        /// Clone
        /// </summary>
        /// <returns>A copy of this object</returns>
        public override BaseMonitoredItemModel Clone() {
            return new DataMonitoredItemModel {
                Id = Id,
                TriggerId = TriggerId,
                StartNodeId = StartNodeId,
                SamplingInterval = SamplingInterval,
                QueueSize = QueueSize,
                DiscardNew = DiscardNew,
                SkipFirst = SkipFirst,
                DataChangeFilter = DataChangeFilter.Clone(),
                AggregateFilter = AggregateFilter.Clone(),
                AttributeId = AttributeId,
                IndexRange = IndexRange,
                MonitoringMode = MonitoringMode,
                DisplayName = DisplayName,
                RelativePath = RelativePath,
                DataSetClassFieldId = DataSetClassFieldId,
                HeartbeatInterval = HeartbeatInterval
            };
        }

        /// <summary>
        /// Equals function
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>If the objects are equal</returns>
        public override bool Equals(object obj) {
            return obj is DataMonitoredItemModel model &&
                base.Equals(obj) &&
                DataSetClassFieldId == model.DataSetClassFieldId &&
                SkipFirst == model.SkipFirst &&
                DataChangeFilter.IsSameAs(model.DataChangeFilter) &&
                AggregateFilter.IsSameAs(model.AggregateFilter) &&
                EqualityComparer<TimeSpan?>.Default.Equals(HeartbeatInterval, model.HeartbeatInterval);
        }

        /// <summary>
        /// Calculate hash code
        /// </summary>
        /// <returns>The hash code</returns>
        public override int GetHashCode() {
            var hash = new HashCode();
            hash.Add(DataSetClassFieldId);
            hash.Add(base.GetHashCode());
            hash.Add(DataChangeFilter);
            hash.Add(AggregateFilter);
            hash.Add(HeartbeatInterval);
            hash.Add(SkipFirst);
            return hash.ToHashCode();
        }
    }
}
