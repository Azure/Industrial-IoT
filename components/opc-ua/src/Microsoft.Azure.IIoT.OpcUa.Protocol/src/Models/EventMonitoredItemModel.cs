// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;

    /// <summary>
    /// Data monitored item
    /// </summary>
    public class EventMonitoredItemModel : BaseMonitoredItemModel {
        /// <summary>
        /// Event filter
        /// </summary>
        public EventFilterModel EventFilter { get; set; }

        /// <summary>
        /// Clone
        /// </summary>
        /// <returns></returns>
        public override BaseMonitoredItemModel Clone() {
            return new EventMonitoredItemModel {
                Id = Id,
                TriggerId = TriggerId,
                StartNodeId = StartNodeId,
                SamplingInterval = SamplingInterval,
                QueueSize = QueueSize,
                DiscardNew = DiscardNew,
                EventFilter = EventFilter.Clone(),
                AttributeId = AttributeId,
                IndexRange = IndexRange,
                MonitoringMode = MonitoringMode,
                DisplayName = DisplayName,
                RelativePath = RelativePath,
            };
        }

        /// <summary>
        /// Compare items
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public override bool IsSameAs(BaseMonitoredItemModel other) {
            var eventMonitoredItemModel = other as EventMonitoredItemModel;

            if (eventMonitoredItemModel == null) {
                return false;
            }
            if (!base.IsSameAs(other)) {
                return false;
            }
            if (!EventFilter.IsSameAs(eventMonitoredItemModel.EventFilter)) {
                return false;
            }
            return true;
        }
    }
}
