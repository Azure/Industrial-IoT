// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models.Events;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Event monitored item
    /// </summary>
    public class EventMonitoredItemModel : BaseMonitoredItemModel {
        /// <summary>
        /// Event filter
        /// </summary>
        public EventFilterModel EventFilter { get; set; }

        /// <summary>
        /// Pending alarm settings
        /// </summary>
        public PendingAlarmsOptionsModel PendingAlarms { get; set; }

        /// <summary>
        /// Clone
        /// </summary>
        /// <returns>A copy of this object</returns>
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
                PendingAlarms = PendingAlarms?.Clone() ?? null
            };
        }

        /// <summary>
        /// Equals function
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>If the objects are equal</returns>
        public override bool Equals(object obj) {
            return obj is EventMonitoredItemModel model &&
                   base.Equals(obj) &&
                   EventFilter.IsSameAs(model.EventFilter) &&
                   PendingAlarms == model.PendingAlarms;
        }

        /// <summary>
        /// Calculate hash code
        /// </summary>
        /// <returns>The hash code</returns>
        public override int GetHashCode() {
            var hash = new HashCode();
            hash.Add(base.GetHashCode());
            hash.Add(EventFilter);
            hash.Add(PendingAlarms);
            return hash.ToHashCode();
        }

        /// <summary>
        /// operator==
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>If the objects are equal</returns>
        public static bool operator ==(EventMonitoredItemModel left, EventMonitoredItemModel right) => EqualityComparer<EventMonitoredItemModel>.Default.Equals(left, right);

        /// <summary>
        /// operator!=
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>If the objects are not equal</returns>
        public static bool operator !=(EventMonitoredItemModel left, EventMonitoredItemModel right) => !(left == right);
    }
}
