// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
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
        /// Equals function
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) {
            return obj is EventMonitoredItemModel model &&
                   base.Equals(obj) &&
                   EventFilter.IsSameAs(model.EventFilter);
        }

        /// <summary>
        /// Calculate hash code
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            var hash = new HashCode();
            hash.Add(base.GetHashCode());
            hash.Add(EventFilter);
            return hash.ToHashCode();
        }

        /// <summary>
        /// operator==
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(EventMonitoredItemModel left, EventMonitoredItemModel right) => EqualityComparer<EventMonitoredItemModel>.Default.Equals(left, right);

        /// <summary>
        /// operator!=
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(EventMonitoredItemModel left, EventMonitoredItemModel right) => !(left == right);
    }
}
